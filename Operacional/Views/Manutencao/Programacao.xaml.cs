using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Operacional.Views.Manutencao;

/// <summary>
/// Interação lógica para Programacao.xam
/// </summary>
public partial class Programacao : UserControl
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public Programacao()
    {
        InitializeComponent();
        DataContext = new ProgramacaoViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ProgramacaoViewModel vm = (ProgramacaoViewModel)DataContext;
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            vm.Programacoes = await vm.GetProgramacoesAsync();
            vm.Aprovados = await vm.GetAprovadosAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show($"Erro ao carregar programações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void EquipeRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        ProgramacaoViewModel vm = (ProgramacaoViewModel)DataContext;
        try
        {
            var linha = e.Row.Item as OperacionalProgramacaoManutencaoModel;
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            await vm.AddProgramacaoAsync(linha);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show($"Erro ao validar a linha: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnImportProgramacaoClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        ProgramacaoViewModel vm = (ProgramacaoViewModel)DataContext;
        var dlg = new OpenFileDialog
        {
            Title = "Selecione a programação em Excel",
            Filter = "Arquivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls",
            Multiselect = false
        };

        bool? result = dlg.ShowDialog();
        if (result == true)
        {
            string arquivoExcel = dlg.FileName;
            var resultado = CarregarProgramacao(arquivoExcel);
            if (resultado.Invalidos.Any())
            {
                // 1) Preparar linhas de texto
                var linhas = resultado.Invalidos
                    .Select(err => $"Linha {err.Linha}: {err.Mensagem}")
                    .ToList();

                // 2) Definir um caminho de arquivo (por exemplo, na pasta temp)
                string caminhoTxt = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "ErrosImportacao.txt");

                try
                {
                    // 3) Escrever o arquivo (sobrescreve se já existir)
                    File.WriteAllLines(caminhoTxt, linhas);

                    // 4) Abrir o arquivo com o aplicativo padrão de .txt
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = caminhoTxt,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                      $"Não foi possível gerar o arquivo de erros:\n{ex.Message}",
                      "Erro ao exportar",
                      MessageBoxButton.OK,
                      MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                    await vm.AddProgramacaoImportacaoAsync(resultado);
                    vm.Programacoes = await vm.GetProgramacoesAsync();

                    MessageBox.Show(
                        $"Importação concluída com sucesso! {resultado.Validos.Count} registros válidos.",
                        "Importação concluída",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    MessageBox.Show(
                        $"Erro ao importar os dados: {ex.Message}",
                        "Erro de importação",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }

    private ResultadoImportacao? CarregarProgramacao(string caminhoArquivo)
    {
        try
        {
            ProgramacaoViewModel vm = (ProgramacaoViewModel)DataContext;
            using var workbook = new XLWorkbook(caminhoArquivo);
            var ws = workbook.Worksheet(1);


            var itens = ws.RangeUsed()
                  .RowsUsed()
                  .Skip(1)
                  .Select(r =>
                  {
                      int linha = r.RowNumber();
                      string sData = r.Cell(1).GetString().Trim();
                      string shopp = r.Cell(2).GetString().Trim();
                      string tipo = r.Cell(3).GetString().Trim();

                      var erros = new List<string>();
                      if (!DateTime.TryParse(sData, out DateTime data))
                          erros.Add("Data inválida");

                      if (string.IsNullOrWhiteSpace(shopp))
                          erros.Add("Shopp vazio");

                      bool isValidShopp = vm.GetValidateSiglaAsync(shopp);
                      if (!isValidShopp && !string.IsNullOrWhiteSpace(shopp))
                          erros.Add($"Shopp '{shopp}' não encontrado nos aprovados");

                      if (string.IsNullOrWhiteSpace(tipo))
                      {
                          erros.Add("Tipo vazio");
                      }
                      else if (!vm.Tipos.Contains(tipo))
                      {
                          erros.Add($"Tipo '{tipo}' inválido. Valores permitidos: {string.Join(", ", vm.Tipos)}");
                      }

                      return new
                      {
                          Linha = linha,
                          Registro = erros.Count == 0
                                     ? new RegistroExcel { Data = data, Shopp = shopp, Tipo = tipo }
                                     : null,
                          Erro = erros.Count == 0
                                     ? null
                                     : string.Join("; ", erros)
                      };
                  })
                  .ToList();

            return new ResultadoImportacao
            {
                Validos = itens
                           .Where(x => x.Erro == null)
                           .Select(x => x.Registro!)
                           .ToList(),
                Invalidos = itens
                             .Where(x => x.Erro != null)
                             .Select(x => new ErroRegistro
                             {
                                 Linha = x.Linha,
                                 Mensagem = x.Erro!
                             })
                             .ToList()
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao ler o arquivo Excel: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        
    }

    private void OnAddFuncoesClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        var selectedItem = manutProgramacao.CurrentItem;
        var programacao = selectedItem as OperacionalProgramacaoManutencaoModel;

        AdicionarFuncoes radWindow = new(programacao.id)
        {
            Width = 600,
            ResizeMode = ResizeMode.NoResize,
            CanMove = false
        };
        //StyleManager.SetTheme(radWindow, new Windows8Theme());
        radWindow.ShowDialog();
    }

    private void OnAddSolicitacoesClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        var selectedItem = manutProgramacao.CurrentItem;
        var programacao = selectedItem as OperacionalProgramacaoManutencaoModel;

        AdicionarSolicitacao radWindow = new(programacao.id)
        {
            Width = 600,
            ResizeMode = ResizeMode.NoResize,
            CanMove = false
        };
        //StyleManager.SetTheme(radWindow, new Windows8Theme());
        radWindow.ShowDialog();
    }

    private async void OnCreatReportClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        try
        {
            ProgramacaoViewModel vm = (ProgramacaoViewModel)DataContext;

            var selectedItem = manutProgramacao.CurrentItem;
            var programacao = selectedItem as OperacionalProgramacaoManutencaoModel;

            await vm.LoadManutencaoSolicitacaoAsync(programacao.id);

            // Cria o documento
            using var doc = DocX.Create(@$"{BaseSettings.CaminhoSistema}Impressos\{programacao.tipo}_{programacao.data:yyyy_MM_dd}.docx");

            // 28.4f = 1cm
            doc.MarginTop = 120.6f;     // = 4,25 cm
            doc.MarginBottom = 21.3f;   // = 0,75 cm
            doc.MarginLeft = 36.068f;   // = 1,27 cm
            doc.MarginRight = 42.6f;    // = 1,5 cm

            // 2) Rodapé com data de geração (esquerda) e “Página X de Y” (direita)
            doc.AddFooters();
            var footer = doc.Footers.Odd;
            // data à esquerda
            var fParaLeft = footer.InsertParagraph();
            fParaLeft.Alignment = Alignment.left;
            fParaLeft.Append("Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            // numeração à direita
            var fParaRight = footer.InsertParagraph();
            fParaRight.Alignment = Alignment.right;
            fParaRight
            .Append("Página ")
            .AppendPageNumber(PageNumberFormat.normal)
            .Append(" de ")
            .AppendPageCount(PageNumberFormat.normal);

            // Cabeçalho
            doc.InsertParagraph($"{programacao.shopp} – {programacao.tipo}")
                .FontSize(16)
                .Font("Calibri")
                .Bold()
                .SpacingAfter(20)
                .Alignment = Alignment.center;

            var p = doc.InsertParagraph();
            // Primeiro trecho “Data: ” com fonte tamanho 10 e cor padrão
            p.Append("Data: ")
             .FontSize(10)
             .Font("Calibri")
             .Bold()
             .Color(Xceed.Drawing.Color.Black);

            // Depois o trecho da data em cor diferente (por ex. vermelho)
            p.Append($"{programacao.data:dd/MM/yyyy}")
             .FontSize(10)
             .Font("Calibri")
             .Bold()
             .Color(Xceed.Drawing.Color.Blue);

            doc.InsertParagraph("Horário de chegada: ")
                .FontSize(10)
                .Font("Calibri")
                .Bold(); 

            doc.InsertParagraph("Horário de saída: ")
                .FontSize(10)
                .Font("Calibri")
                .SpacingAfter(20)
                .Bold();

            int totalRows = 2;
            int totalCols = 2;

            // 2) Cria a tabela com estilo de grade
            var table = doc.AddTable(totalRows, totalCols);
            table.Design = TableDesign.TableGrid;
            table.SetColumnWidth(1, 50f);
            table.Rows[0].MergeCells(0, totalCols - 1);
            var titleCell = table.Rows[0].Cells[0];
            titleCell.FillColor = Xceed.Drawing.Color.Gray;
            titleCell.Paragraphs[0]
                 .Append("EQUIPE CONTRATADA")
                 .Bold()
                 .Font("Calibri")
                 .FontSize(12)
                 .Alignment = Alignment.center;

            var headerRow1 = table.Rows[1].Cells[0];
            headerRow1.FillColor = Xceed.Drawing.Color.LightGray;
            headerRow1.Paragraphs[0]
                 .Append("FUNÇÃO")
                 .Font("Calibri")
                 .Bold()
                 .FontSize(10)
                 .Alignment = Alignment.center;

            var headerRow2 = table.Rows[1].Cells[1];
            headerRow2.FillColor = Xceed.Drawing.Color.LightGray;
            headerRow2.Paragraphs[0]
                 .Append("QTD")
                 .Font("Calibri")
                 .Bold()
                 .FontSize(10)
                 .Alignment = Alignment.center;

            await vm.LoadManutencaoFuncoesAsync(programacao.id);
            foreach (var funcao in vm.ManutencaoFuncoes)
            {
                var row = table.InsertRow();
                // Preenche as células
                row.Cells[0].Paragraphs[0]
                   .Append(funcao.funcao)
                   .Font("Calibri")
                   .FontSize(8);
                row.Cells[1].Paragraphs[0]
                   .Append(funcao.qtd.ToString())
                   .Font("Calibri")
                   .FontSize(8)
                   .Alignment = Alignment.center;
            }

            doc.InsertTable(table);
            doc.InsertParagraph("")
                .SpacingAfter(20);

            var p2 = doc.InsertParagraph();
            p2.Append("Por favor, anote se a solicitação foi ou não atendida e envie as fotos comparativas (antes e depois da manutenção realizada) para o whatsapp ")
                .FontSize(10)
                .Font("Calibri")
                .Color(Xceed.Drawing.Color.Black);
            p2.Append("11 94014 - 5729, ")
                .Font("Calibri")
                .FontSize(10)
                .Bold()
                .Color(Xceed.Drawing.Color.Red);
            p2.Append("identificadas como MANUTENÇÃO ")
                .FontSize(10)
                .Font("Calibri");
            p2.Append("SIGLA DIA/MÊS.")
               .FontSize(10)
               .Font("Calibri")
               .Bold()
               .SpacingAfter(20)
               .Color(Xceed.Drawing.Color.Blue);

            // Seção Cliente
            doc.InsertParagraph("Solicitação do Cliente")
                .FontSize(12)
                .Bold()
                .Font("Calibri")
                .SpacingAfter(20);

            foreach (var item in vm.Solicitacoes.Where(x => x.Tipo == "CLIENTE").OrderBy(x=> x.Item) .ToList())
            {
                /*
                doc.InsertParagraph($"{item.Item}. {item.Solicitacao}")
                    .FontSize(10)
                    .Font("Calibri")
                    .SpacingAfter(10);
                */

                var pCli = doc.InsertParagraph();
                pCli.Append($"{item.Item}. ")
                    .FontSize(10)
                    .Font("Calibri")
                    .Color(Xceed.Drawing.Color.Black);

                pCli.Append($"{item.Solicitacao}")
                    .FontSize(10)
                    .Bold()
                    .Font("Calibri")
                    .Color(Xceed.Drawing.Color.DarkBlue)
                    .SpacingAfter(10);

                foreach (var imgPath in item.Imagens)
                {
                    if (string.IsNullOrWhiteSpace(imgPath.CaminhoImagem) || !File.Exists(imgPath.CaminhoImagem))
                        continue;

                    var img = doc.AddImage(imgPath.CaminhoImagem);
                    
                    var pic = img.CreatePicture();
                    // opcional: ajustar tamanho
                    pic.Width = 500; pic.Height = 300;

                    

                    // cada imagem fica em seu próprio parágrafo
                    doc.InsertParagraph()
                        .AppendPicture(pic);
                }
            }

            // Seção Cipolatti
            doc.InsertParagraph("Solicitação da Cipolatti")
                .FontSize(12)
                .Font("Calibri")
                .Bold()
                .SpacingAfter(20)
                .InsertPageBreakBeforeSelf();
            foreach (var item in vm.Solicitacoes.Where(x => x.Tipo == "CIPOLATTI").OrderBy(x => x.Item).ToList())
            {
                /*
                doc.InsertParagraph($"{item.Item}. {item.Solicitacao}")
                    .FontSize(10)
                    .Font("Calibri")
                    .SpacingAfter(10);
                */
                var pCip = doc.InsertParagraph();
                pCip.Append($"{item.Item}. ")
                    .FontSize(10)
                    .Font("Calibri")
                    .Color(Xceed.Drawing.Color.Black);

                pCip.Append($"{item.Solicitacao}")
                    .FontSize(10)
                    .Bold()
                    .Font("Calibri")
                    .Color(Xceed.Drawing.Color.DarkBlue)
                    .SpacingAfter(10);

                foreach (var imgPath in item.Imagens)
                {
                    if (string.IsNullOrWhiteSpace(imgPath.CaminhoImagem) || !File.Exists(imgPath.CaminhoImagem))
                        continue;

                    var img = doc.AddImage(imgPath.CaminhoImagem);
                    var pic = img.CreatePicture();
                    // opcional: ajustar tamanho
                    pic.Width = 500; pic.Height = 300;

                    // cada imagem fica em seu próprio parágrafo
                    doc.InsertParagraph()
                        .AppendPicture(pic);
                }
            }

            // Salva e fecha
            doc.Save();

            Process.Start(new ProcessStartInfo
            {
                FileName = @$"{BaseSettings.CaminhoSistema}Impressos\{programacao.tipo}_{programacao.data:yyyy_MM_dd}.docx",  // caminho completo do .docx
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar relatório: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}

public partial class ProgramacaoViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<OperacionalProgramacaoManutencaoModel> programacoes;
    [ObservableProperty]
    private ObservableCollection<ProducaoAprovadoModel> aprovados;
    [ObservableProperty]
    private ObservableCollection<string> tipos = ["MANUTENÇÃO PROGRAMADA", "MANUTENÇÃO CORRETIVA", "FINALIZAÇÃO DE MONTAGEM", "COMPLEMENTO"];
    [ObservableProperty]
    private ObservableCollection<string> equipes;
    [ObservableProperty]
    private ObservableCollection<SolicitacaoManutencaoDTO> solicitacoes;
    [ObservableProperty]
    private ObservableCollection<OperacionalPessoasManutencaoModel> manutencaoFuncoes;

    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public async Task<ObservableCollection<OperacionalProgramacaoManutencaoModel>> GetProgramacoesAsync()
    {
        using var _db = new Context();
        var programacoes = await _db.OperacionalProgramacaoManutencoes
            .OrderBy(x => x.data)
            .ThenBy(x => x.shopp)
            .ThenBy(x => x.tipo)
            .ToListAsync();
        return new ObservableCollection<OperacionalProgramacaoManutencaoModel>(programacoes);
    }   

    public async Task<ObservableCollection<ProducaoAprovadoModel>> GetAprovadosAsync()
    {
        using var _db = new Context();
        var aptrovados = await _db.ProducaoAprovados
            .OrderBy(x => x.sigla_serv)
            .ToListAsync();
        return new ObservableCollection<ProducaoAprovadoModel>(aptrovados);
    }

    public async Task<ObservableCollection<string>> GetEquipesAsync()
    {
        using var context = new Context();
        var query = from equipe in context.Equipes
                    join valores in context.EquipePrevisoes
                    on equipe.id equals valores.id_equipe
                    group valores by new { valores.id_equipe, equipe.equipe_e } into g
                    select g.Key.equipe_e;
        return new ObservableCollection<string>(await query.ToListAsync());
    }

    public bool GetValidateSiglaAsync(string sigla)
    {
        using var _db = new Context();
        var aprovado = _db.ProducaoAprovados.FirstOrDefault(x => x.sigla_serv == sigla);
        //var funcionario = await _db.ComercialClientes.FirstOrDefaultAsync(x => x.sigla == aprovado.sigla);
        return aprovado != null;
    }

    public async Task AddProgramacaoImportacaoAsync(ResultadoImportacao importacao)
    {
        using var _db = new Context();
        foreach (var registro in importacao.Validos)
        {
            var programacao = _db.OperacionalProgramacaoManutencoes.FirstOrDefault(x => x.shopp == registro.Shopp && x.data == registro.Data && x.tipo == registro.Tipo);
            var aprovado = _db.ProducaoAprovados.FirstOrDefault(x => x.sigla_serv == registro.Shopp);
            var cliente = await _db.ComercialClientes.FirstOrDefaultAsync(x => x.sigla == aprovado.sigla);
            if (programacao == null)
            {
                _db.OperacionalProgramacaoManutencoes.Add(new OperacionalProgramacaoManutencaoModel
                {
                    data = registro.Data,
                    shopp = registro.Shopp,
                    cidade = cliente?.cidade ?? "N/A",
                    est = cliente?.est ?? "N/A",
                    tipo = registro.Tipo,
                    cadastrado_por = BaseSettings.Username,   
                    data_cadastro = DateTimeOffset.Now
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task AddProgramacaoAsync(OperacionalProgramacaoManutencaoModel model)
    {
        using var _db = new Context();

        var aprovado = _db.ProducaoAprovados.FirstOrDefault(x => x.sigla_serv == model.shopp);
        var cliente = await _db.ComercialClientes.FirstOrDefaultAsync(x => x.sigla == aprovado.sigla);

        model.cidade = cliente?.cidade ?? "N/A";
        model.est = cliente?.est ?? "N/A";

        var modelExistente = await _db.OperacionalProgramacaoManutencoes.FindAsync(model.id);
        if (modelExistente == null)
        {

            _db.OperacionalProgramacaoManutencoes.Add(model);
        }
        else
        {
            model.alterado_por = BaseSettings.Username;
            model.data_alteracao = DateTimeOffset.Now;
            _db.Entry(modelExistente).CurrentValues.SetValues(model); 
        }
        await _db.SaveChangesAsync();
    }

    public async Task LoadManutencaoSolicitacaoAsync(long idProgramacao)
    {
        using var context = new Context();

        var solicitacoes = await context.OperacionalSolicitacaoManutencoes
            .Where(x => x.id_programacao == idProgramacao)
            .ToListAsync();

        var fotos = await context.OperacionalSolicitacaoManutencaoFotos
            .Where(x => solicitacoes.Select(s => s.id).Contains(x.id_solicitacao))
            .ToListAsync();

        // Mapeia entidades para DTOs
        Solicitacoes = new ObservableCollection<SolicitacaoManutencaoDTO>(
            solicitacoes.Select(solicitacao => new SolicitacaoManutencaoDTO
            {
                Id = solicitacao.id,
                IdProgramacao = solicitacao.id_programacao,
                Tipo = solicitacao.tipo,
                Item = solicitacao.item,
                Solicitacao = solicitacao.solicitacao,
                Imagens = [
                    .. fotos.Where(foto => foto.id_solicitacao == solicitacao.id)
                    .Select(foto => new SolicitacaoManutencaoFotoDTO
                    {
                        Id = foto.id,
                        IdSolicitacao = foto.id_solicitacao,
                        CaminhoImagem = foto.caminho_imagem
                    })]
            }));
    }

    public async Task LoadManutencaoFuncoesAsync(long idProgramacao)
    {
        using var context = new Context();
        ManutencaoFuncoes = new ObservableCollection<OperacionalPessoasManutencaoModel>(
            await context.OperacionalPessoasManutencoes
                .Where(x => x.id_programacao == idProgramacao)
                .ToListAsync());
    }
}

public class RegistroExcel
{
    public DateTime Data { get; set; }
    public string Shopp { get; set; }
    public string Tipo { get; set; }
}

public class ErroRegistro
{
    public int Linha { get; set; }
    public string Mensagem { get; set; }
}

public class ResultadoImportacao
{
    public List<RegistroExcel> Validos { get; set; } = [];
    public List<ErroRegistro> Invalidos { get; set; } = [];
}