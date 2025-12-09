using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using Operacional.Views;
using Operacional.Views.Cronograma;
using Operacional.Views.Despesa;
using Operacional.Views.Documentos;
using Operacional.Views.EquipeExterna;
using Operacional.Views.EquipeExterna.Consultas;
using Operacional.Views.Manutencao;
using Operacional.Views.Transporte;
using Producao;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;
using SizeMode = Syncfusion.SfSkinManager.SizeMode;

namespace Operacional
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public MainWindow()
        {
            InitializeComponent();

            StyleManager.ApplicationTheme = new Office2016Theme();

            VisualStyles visualStyle = VisualStyles.Default;
            Enum.TryParse("Metro", out visualStyle);
            if (visualStyle != VisualStyles.Default)
            {
                SfSkinManager.ApplyStylesOnApplication = true;
                SfSkinManager.SetVisualStyle(this, visualStyle);
                SfSkinManager.ApplyStylesOnApplication = false;
            }

            SizeMode sizeMode = SizeMode.Default;
            Enum.TryParse("Default", out sizeMode);
            if (sizeMode != SizeMode.Default)
            {
                SfSkinManager.ApplyStylesOnApplication = true;
                SfSkinManager.SetSizeMode(this, sizeMode);
                SfSkinManager.ApplyStylesOnApplication = false;
            }

            //var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
            //if (appSettings[0].Length > 0)
            //   BaseSettings.Username = appSettings[0];

            txtUsername.Text = BaseSettings.Username;
            txtDataBase.Text = BaseSettings.Database;
        }


        private void OnAlterarUsuario(object sender, MouseButtonEventArgs e)
        {
            Login window = new();
            window.ShowDialog();

            try
            {
                var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
                BaseSettings.Username = appSettings[0];
                txtUsername.Text = BaseSettings.Username;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RadWindow.Prompt(new DialogParameters()
            {
                Header = "Ano Sistema",
                Content = "Alterar o Ano do Sistema",
                Closed = (object sender, WindowClosedEventArgs e) =>
                {
                    if (e.PromptResult != null)
                    {
                        BaseSettings.Database = e.PromptResult;
                        txtDataBase.Text = BaseSettings.Database;
                        _mdi.Items.Clear();
                    }
                }
            });
        }

        private void _mdi_CloseAllTabs(object sender, CloseTabEventArgs e)
        {
            _mdi.Items.Clear();
        }

        private void _mdi_CloseButtonClick(object sender, CloseButtonEventArgs e)
        {
            var tab = (DocumentContainer)sender;
            _mdi.Items.Remove(tab.ActiveDocument);
        }

        public void adicionarFilho(object filho, string title, string name)
        {
            var doc = ExistDocumentInDocumentContainer(name);
            if (doc == null)
            {
                doc = (FrameworkElement?)filho;
                DocumentContainer.SetHeader(doc, title);
                doc.Name = name.ToLower();
                _mdi.Items.Add(doc);
            }
            else
            {
                //_mdi.RestoreDocument(doc as UIElement);
                _mdi.ActiveDocument = doc;
            }
        }

        private FrameworkElement ExistDocumentInDocumentContainer(string name_)
        {
            foreach (FrameworkElement element in _mdi.Items)
            {
                if (name_.ToLower() == element.Name)
                {
                    return element;
                }
            }
            return null;
        }

        private void OnCadastroTransportadoraClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroTransportadora(), "CADASTRO TRANSPORTADORA", "CADASTRO_TRANSPORTADORA");
        }

        private void OnTransporteMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TransporteMontagem(), "TRANSPORTES MONTAGEM", "TRANSPORTE_MONTAGEM");
        }

        private void OnTransporteDesmontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TransporteDesmontagem(), "TRANSPORTES DESMONTAGEM", "TRANSPORTE_DESMONTAGEM");
        }

        private void OnCargaMontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CargaMontagem(), "CARGAS MONTAGEM", "CARGAS_MONTAGEM");
        }


        private void OnCargaDesmontagemClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CargaDesmontagem(), "CARGAS DESMONTAGEM", "CARGAS_DESMONTAGEM");
        }


        private void OnDataEfetivaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new DataEfetivaView(), "DATA EFETIVA", "DATA_EFETIVA");
        }

        private async void OnQryCargaMontgemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using Context context = new();
                var retorno = await context.QryCargasMontagem.AsNoTracking().ToListAsync();
                

                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;

                application.DefaultVersion = ExcelVersion.Xlsx;

                //Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                //worksheet.IsGridLinesVisible = false;
                worksheet.ImportData(retorno, 1, 1, true);


                // Obter o intervalo da coluna "Data" e "data_de_expedicao"
                int rowCount = retorno.Count;
                // Colunas que devem mudar de cor quando A2 <> B2
                string[] columnsToFormat = { "A", "B", "C" };

                foreach (string col in columnsToFormat)
                {
                    string range = $"{col}2:{col}{rowCount + 1}"; // Exemplo: "A2:A6"

                    // Criar a formatação condicional para cada coluna
                    IConditionalFormats conditionalFormats = worksheet.Range[range].ConditionalFormats;
                    IConditionalFormat condition = conditionalFormats.AddCondition();

                    // Definir o tipo como Fórmula
                    condition.FormatType = ExcelCFType.Formula;
                    condition.FirstFormula = $"=$A2<>$B2";  // Baseando-se na diferença entre A2 e B2

                    // Definir a cor de fundo quando a condição for verdadeira
                    condition.BackColorRGB = Color.Yellow;
                }

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_CARGAS_MONTAGEM.xlsx");
                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_CARGAS_MONTAGEM.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnDataMontagemClick(object sender, RoutedEventArgs e)
        {
            try
            {


                string sql = @"
                    SELECT 
                        sigla, 
                        siglaserv, 
                        grupo, 
                        nome, 
                        cidade, 
                        post_data_alterado, 
                        data_de_expedicao, 
                        est, 
                        dsl_inicio_montagem, 
                        dsl_termino_montagem, 
                        fecha_data_montagem, 
                        data_inauguracao, 
                        data_informada_cliente, 
                        diarias_cronograma, 
                        data_pedido_cliente_inicio_montagem, 
                        data_pedido_cliente_termino_montagem, 
                        contrato_inicio_mont, 
                        contrato_final_mont, 
                        ano
	                FROM operacional.qry_datas_montagem;";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];

                // Import the DataTable
                worksheet.ImportDataTable(dataTable, true, 1, 1);

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\DATAS-MONTAGEM.xlsx");

                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\DATAS-MONTAGEM.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnQryCargaDesmontagemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                //using Context context = new();
                //var retorno = await context.QryCargasDesmontagem.AsNoTracking().ToListAsync();

                using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
                var sql = @"SELECT * FROM operacional.qrytranspdesmont_detalhes ORDER BY data_chegada_shopping, sigla_serv";
                var lista = (await conn.QueryAsync<TranspDesmontDetalheModel>(sql)).ToList();
                var retorno = new ObservableCollection<TranspDesmontDetalheModel>(lista);

                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;

                application.DefaultVersion = ExcelVersion.Xlsx;

                //Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                //worksheet.IsGridLinesVisible = false;
                worksheet.ImportData(retorno, 1, 1, true);

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_CARGAS_DESMONTAGEM.xlsx");
                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_CARGAS_DESMONTAGEM.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenTiposRelatoriosClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new TipoRelatorio(), "CADASTRO RELATÓRIOS", "CADASTRO_RELATORIOS");
        }

        private void OnOpenCadastroFuncionarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroFuncionario(), "CADASTRO FUNCIONÁRIO", "CADASTRO_FUNCIONARIO");
        }

        private void OnOpenCadastroDespesaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroDespesa(), "CADASTRO DESPESAS", "CADASTRO_DESPESAS");
        }

        private void OnCronogramaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Cronograma(), "CRONOGRAMA", "CRONOGRAMA");
        }

        private async void OnFuncoesCronogramaClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using Context context = new();
                var retorno = await context.OperacionalNoitescronogPessoas
                    .Where(p => p.qtd_pessoas>0)
                    .OrderBy(p => p.sigla)
                    .ThenBy(p => p.fase)
                    .ThenBy(p => p.funcao)
                    .AsNoTracking().ToListAsync();

                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;

                application.DefaultVersion = ExcelVersion.Xlsx;

                //Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                //worksheet.IsGridLinesVisible = false;
                worksheet.ImportData(retorno, 1, 1, true);

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_FUNCOES_CRONOGRAMA.xlsx");
                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\QUERY_FUNCOES_CRONOGRAMA.xlsx")
                {
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenCadastroEquipesClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroEquipe(), "CADASTRO DE EQUIPES", "CADASTRO_EQUIPES");
        }

        private void OnOpenCadastroOrcamentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroOrcamento(), "CADASTRO DE ORÇAMENTO", "CADASTRO_ORCAMENTO");
        }

        private void OnOpenUsuarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new CadastroUsuario(), "CADASTRO DE USUÁRIOS", "CADASTRO_USUARIOS");
        }

        private void OnContatoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Contato(), "CONTATOS", "EQUIPE_EXTERNA_CONTATOS");
        }

        private async void OnRelatorioPrevisaoValoresClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ValoresCronograma(), "PREVISÃO VALORES CRONOGRAMA", "PREVISAO_VALORES_CRONOGRAMA");
            
            /*
            try
            {
                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);

                string sql = @"
                    SELECT  
                            sigla, qtd_pessoas, qtd_noites, equipe, 
                            fase, funcao, valor_ano_atual, valor_total, 
                            lanche, transporte, id_equipe, indice_pessoas_noite, 
                            razaosocial, vai_equipe
	                FROM equipe_externa.qry_previsao_valores_cronograma;
                ";

                var result = await connection.QueryAsync<PrevisaoValorCronogramaDTO>(sql);
                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];
                worksheet.ImportData(result, 1, 1, true);
                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\PrevisaoValores.xlsx");
                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\PrevisaoValores.xlsx")
                {
                    UseShellExecute = true
                });
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (PostgresException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NpgsqlException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            */
        }

        private void OnComparacaoPrevisaoRealizadoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ComparacaoPrevisarLancamento(), "COMPARAÇÃO PREVISÃO REALIZADO", "COMPARACAO_PREVISAO_REALIZADO");
        }

        private void OnOpenNotasPagamentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaPagamento(), "NOTAS PARA PAGAMENTO LANÇAMENTO", "NOTAS_PAGAMENTO_LANCAMENTO");
        }

        private void OnOpenNotasPagamentoConsultaClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaPagamentoResumo(0), "NOTAS PARA PAGAMENTO RESUMO", "NOTAS_PAGAMENTO_RESUMO");
        }

        private void OnOpenNotasDespesasClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new NotaDespesa(), "NOTAS DE DESPESAS", "NOTAS_DESPESA");
        }

        private void OnOpenProgramacaoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new Programacao(), "PROGRAMAÇÃO MANUTENÇÃO", "PROGRAMACAO_MANUTENCAO");
        }

        private async void OnOpenConsultaGeralClick(object sender, RoutedEventArgs e)
        {
            try
            {


                string sql = @"
                    SELECT 
	                    tbl_programacao_manutencao.id, shopp, cidade, est, data, 
	                    tbl_programacao_manutencao.tipo, funcao, qtd, tbl_solicitacao_manutencao.tipo AS solicitado, 
	                    item, solicitacao, caminho_imagem, resp_atendimento, nome_equipe, qtde_pessoa, obs_retorno, 
	                    cadastrado_por, data_cadastro, alterado_por, data_alteracao 
                    FROM operacional.tbl_programacao_manutencao
                    LEFT JOIN operacional.tbl_pessoas_manutencao ON tbl_programacao_manutencao.id = tbl_pessoas_manutencao.id_programacao
                    LEFT JOIN operacional.tbl_solicitacao_manutencao ON tbl_programacao_manutencao.id = tbl_solicitacao_manutencao.id_programacao
                    LEFT JOIN operacional.tbl_solicitacao_manutencao_foto ON operacional.tbl_solicitacao_manutencao.id = tbl_solicitacao_manutencao_foto.id_solicitacao;
                    ";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                /*
                using ExcelEngine excelEngine = new();
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // Create a workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];

                // Import the DataTable
                worksheet.ImportDataTable(dataTable, true, 1, 1);

                workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\CONSULTA-GERAL-MANUTENCAO.xlsx");

                Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\CONSULTA-GERAL-MANUTENCAO.xlsx")
                {
                    UseShellExecute = true
                });
                */

                var path = @$"{BaseSettings.CaminhoSistema}Impressos\CONSULTA-GERAL-MANUTENCAO.xlsx";

                // Salva em background (ClosedXML é síncrono)
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    wb.Worksheets.Add(dataTable, "CONSULTA GERAL MANUTENCAO");

                    var ws = wb.Worksheet(1);
                    var used = ws.RangeUsed();
                    if (used != null)
                    {
                        ws.Row(1).Style.Font.Bold = true;
                        //used.SetAutoFilter();
                        ws.Columns().AdjustToContents();
                    }

                    wb.SaveAs(path);
                });

                // abrir
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRelatorioDiarioWebClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new RelatorioDiarioWeb(), "RELATÓRIO DIÁRIO WEB", "RELATORIO_DIARIO_WEB");
        }

        private void OnRelatorioNoturnoDiarioClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new RelatorioNoturnoDiario(), "RELATÓRIO NOTURNO DIÁRIO", "RELATORIO_NOTURNO_DIARIO");
        }

        private void OnOpenControleDocumentoClick(object sender, RoutedEventArgs e)
        {
            adicionarFilho(new ControleDocumento(), "CONTROLE DOCUMENTOS", "CONTROLE_DOCUMENTOS");
        }

        private async void OnControleDocumentoPreenchidosClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string sql = @"
                    SELECT 
	                    cli.id,
	                    cli.sigla,
	                    doc.item,
	                    doc.quando_enviar,
	                    doc.responsavel_liberacao,
	                    doc.email_responsavel_liberacao,
	                    cli.fecha,
	                    cli.direcionado_resp,
	                    cli.direcionado_resp_por,
	                    cli.direcionado_resp_em	em_analise,
	                    cli.em_analise_por	em_analise_em,
	                    cli.concluido,
	                    cli.concluido_por,
	                    cli.concluido_em,
	                    cli.enviado,
	                    cli.enviado_por,
	                    cli.enviado_em
                    FROM operacional.tblcontrole_documento doc
                    JOIN operacional.tblcontrole_documento_cliente cli ON doc.id = cli.id_documento
                    ORDER BY cli.sigla, doc.quando_enviar, doc.responsavel_liberacao;
                ";

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
                await connection.OpenAsync();

                var dataTable = new System.Data.DataTable();
                using (var command = new NpgsqlCommand(sql, connection))
                using (var dataAdapter = new NpgsqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }

                await connection.CloseAsync();

                var path = @$"{BaseSettings.CaminhoSistema}Impressos\CONTROLE-DOCUMENTOS.xlsx";

                // Salva em background (ClosedXML é síncrono)
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    wb.Worksheets.Add(dataTable, "CONTROLE DOCUMENTOS");

                    var ws = wb.Worksheet(1);
                    var used = ws.RangeUsed();
                    if (used != null)
                    {
                        ws.Row(1).Style.Font.Bold = true;
                        //used.SetAutoFilter();
                        ws.Columns().AdjustToContents();
                    }

                    wb.SaveAs(path);
                });

                // abrir
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            }
            catch (DbUpdateException ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)  // Para qualquer outro erro
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}