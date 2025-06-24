using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Operacional.Views.Cronograma;

/// <summary>
/// Interação lógica para Cronograma.xam
/// </summary>
public partial class Cronograma : UserControl
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;
    private string _tipoCronograma;

    public Cronograma()
    {
        InitializeComponent();
        DataContext = new CronogramaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            vm.Aprovados = await vm.GetAprovadosAsync();
            vm.Funcoes = await vm.GetFuncoesAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var aprovado = e.AddedItems[0] as ProducaoAprovadoModel;
        if (aprovado.sigla_serv.StartsWith(aprovado.sigla) && aprovado.sigla_serv.Length > aprovado.sigla.Length)
        {
            btnSigla.Visibility = Visibility.Visible;
            btnCompleto.Visibility = Visibility.Collapsed;
        }
        else
        {
            btnCompleto.Visibility = Visibility.Visible;
            btnSigla.Visibility = Visibility.Visible;
        }
    }

    private async void OnOpenSigla(object sender, RoutedEventArgs e)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            if (cmbAprovados.SelectedItem is not ProducaoAprovadoModel aprovado)
            {
                MessageBox.Show("Selecione um aprovado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _tipoCronograma = "SIGLA";
            await vm.AddCronoBySiglaAsync(aprovado.sigla_serv);
            vm.ViewCronogramas = await vm.GetViewCronogramasAsync(aprovado.sigla_serv);
            vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralAsync(aprovado.sigla_serv);
            await vm.AddOperacionalNoitescronogPessoasAsync(aprovado.sigla_serv, "MONTAGEM");
            vm.NoitescronogPessoas = await vm.GetOperacionalNoitescronogPessoasAsync(aprovado.sigla_serv);
            vm.NoitescronogPessoasManutencao = await vm.GetOperacionalNoitescronogPessoasManutencaoAsync(aprovado.sigla_serv);
            vm.NoitesCronogPessoasManutencaoExtra = await vm.GetOperacionalNoitescronogPessoasManutencaoExtraAsync(aprovado.sigla_serv);
        }
        catch (PostgresException ex)
        {
            // Erro específico do PostgreSQL
            MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (NpgsqlException ex)
        {
            // Erro geral de conexão com PostgreSQL
            MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // Qualquer outro erro
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnOpenCompleto(object sender, RoutedEventArgs e)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            if (cmbAprovados.SelectedItem is not ProducaoAprovadoModel aprovado)
            {
                MessageBox.Show("Selecione um aprovado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _tipoCronograma = "COMPLETO";
            await vm.AddCronoBySiglaAsync(aprovado.sigla);
            vm.ViewCronogramas = await vm.GetViewCronogramasAsync(aprovado.sigla);
            vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralAsync(aprovado.sigla);
            await vm.AddOperacionalNoitescronogPessoasAsync(aprovado.sigla, "MONTAGEM");
            vm.NoitescronogPessoas = await vm.GetOperacionalNoitescronogPessoasAsync(aprovado.sigla);
            vm.NoitescronogPessoasManutencao = await vm.GetOperacionalNoitescronogPessoasManutencaoAsync(aprovado.sigla);
            vm.NoitesCronogPessoasManutencaoExtra = await vm.GetOperacionalNoitescronogPessoasManutencaoExtraAsync(aprovado.sigla);
        }
        catch (PostgresException ex)
        {
            // Erro específico do PostgreSQL
            MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (NpgsqlException ex)
        {
            // Erro geral de conexão com PostgreSQL
            MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // Qualquer outro erro
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Crono_CurrentCellChanged(object sender, EventArgs e)
    {
        var grid = sender as DataGrid;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
        if (grid.CurrentItem is ViewCronogramaModel linha)
        {
            try
            {
                var vm = (CronogramaViewModel)DataContext;
                await vm.AtualizarPessoasNoiteCronograma(linha);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }
    }

    private async void CronoRowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            var aprovado = cmbAprovados.SelectedItem as ProducaoAprovadoModel;
            var sigla = _tipoCronograma == "COMPLETO" ? aprovado.sigla : aprovado.sigla_serv;
            if (e.Row.Item is ViewCronogramaModel linha)
            {
                await vm.AtualizarPessoasNoiteCronograma(linha);
                vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralAsync(sigla);
            }

        }
        catch (PostgresException ex)
        {
            MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (NpgsqlException ex)
        {
            MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        try
        {
            var dataGrid = sender as DataGrid;
            // Forçar o commit da edição antes de pegar os dados
            if (e.EditAction == DataGridEditAction.Commit)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            var linha = e.Row.Item as OperacionalNoitescronogPessoaFuncaoModel;
            await vm.AtualizarPessoasNoiteFuncao(linha);

        }
        catch (PostgresException ex)
        {
            MessageBox.Show($"Erro do banco: {ex.MessageText}\nDetalhe: {ex.Detail}\nLocal: {ex.Where}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (NpgsqlException ex)
        {
            MessageBox.Show($"Erro do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DataGridPessoasManutencao_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (cmbAprovados.SelectedItem is not ProducaoAprovadoModel aprovado)
        {
            MessageBox.Show("Selecione um aprovado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Verifica se a sigla já existe
        e.NewObject = new OperacionalNoitescronogPessoaFuncaoModel
        {
            sigla = _tipoCronograma == "COMPLETO" ? aprovado.sigla : aprovado.sigla_serv,
            fase = "MANUTENÇÃO PROGRAMADA"
        };

    }

    private void RadGridView_AddingNewDataItem(object sender, GridViewAddingNewEventArgs e)
    {
        if (cmbAprovados.SelectedItem is not ProducaoAprovadoModel aprovado)
        {
            MessageBox.Show("Selecione um aprovado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Verifica se a sigla já existe
        e.NewObject = new OperacionalNoitescronogPessoaFuncaoModel
        {
            sigla = _tipoCronograma == "COMPLETO" ? aprovado.sigla : aprovado.sigla_serv,
            fase = "EXTRA"
        };
    }

    private async void RadGridView_RowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        if (e.Row.Item is OperacionalNoitescronogPessoaFuncaoModel linha)
        {
            try
            {
                var vm = (CronogramaViewModel)DataContext;
                await vm.AtualizarPessoasNoiteFuncao(linha);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }
    }

    private void RadGridView_CellValidating(object sender, GridViewCellValidatingEventArgs e)
    {
        if (e.Cell.Column.UniqueName == "funcaoCombo")
        {
            if (e.NewValue == null)
            {
                e.IsValid = false;
                e.ErrorMessage = "Prenncha a função";
            }
        }
    }

    private void OnCronogramaCoordenadorClick(object sender, RoutedEventArgs e)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            var aprovado = cmbAprovados.SelectedItem as ProducaoAprovadoModel;

            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            // Abre o arquivo modelo
            FileStream inputStream = new(@$"{BaseSettings.CaminhoSistema}Modelos\CRONOGRAMA_COORDENADOR.xlsx", FileMode.Open, FileAccess.Read);
            IWorkbook workbook = application.Workbooks.Open(inputStream);
            IWorksheet worksheet = workbook.Worksheets[0];
            // Preenche células fixas
            worksheet.Range["C1"].Text = @$"CRONOGRAMA DE MONTAGEM NATAL {BaseSettings.Database} {Environment.NewLine} {aprovado.nome} - {aprovado.sigla}";
    
            char coluna = 'E';
            int linhaInicial = 8; // Inserir a partir da linha 8
            foreach (var item in vm.CronogramaTotalGerais.Where(x=> !x.status.Contains("TOTAL")))
            {
                var valores = new double?[]
                {
                    item.sn1, item.sn2, item.sn3, item.sn4, item.sn5, item.sn6, item.sn7, item.sn8,
                    item.sn9, item.sn10, item.sn11, item.sn12, item.sn13, item.sn14, item.sn15, item.sn16
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    var cell = worksheet.Range[$"{(char)(coluna + i)}{linhaInicial}"];
                    if (valores[i].HasValue)
                        cell.Number = valores[i].Value;
                    else
                        cell.Value = ""; // ou cell.Clear();
                }

                linhaInicial++;
            }

            linhaInicial = 7; // Inserir a partir da linha 7
            foreach (var item in vm.ViewCronogramas)
            {
                worksheet.Range[$"A{linhaInicial}"].Text = item.item;
                worksheet.Range[$"B{linhaInicial}"].Text = item.localitem;
                worksheet.Range[$"C{linhaInicial}"].Text = item.descricao;
                worksheet.Range[$"D{linhaInicial}"].Number = Convert.ToDouble(item.qtd);

                var valores = new double?[]
                {
                    item.n1, item.n2, item.n3, item.n4, item.n5, item.n6, item.n7, item.n8,
                    item.n9, item.n10, item.n11, item.n12, item.n13, item.n14, item.n15, item.n16
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    var cell = worksheet.Range[$"{(char)(coluna + i)}{linhaInicial}"];
                    if (valores[i].HasValue)
                        cell.Number = valores[i].Value;
                    else
                        cell.Value = ""; // ou cell.Clear();
                }

                worksheet.Range["U" + linhaInicial].Text = item.obs_coordenador;
                linhaInicial++;
                worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
            }
            worksheet.DeleteRow(linhaInicial, 1);

            linhaInicial = 2; // Inserir a partir da linha 2
            foreach (var item in vm.NoitescronogPessoas.Where(x => x.qtd_pessoas > 0))
            {
                worksheet.Range["V" + linhaInicial].Text = item.funcao;
                worksheet.Range["W" + linhaInicial].Number = Convert.ToDouble(item.qtd_pessoas);

                linhaInicial++;
                //worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
            }
            worksheet.UsedRange.AutofitRows();
            // Salva como novo arquivo
            FileStream outputStream = new(@$"{BaseSettings.CaminhoSistema}Impressos\CRONOGRAMA_COORDENADOR-{aprovado.sigla}.xlsx", FileMode.Create, FileAccess.Write);
            workbook.SaveAs(outputStream);

            workbook.Close();
            inputStream.Close();
            outputStream.Close();

            Process.Start("explorer", @$"{BaseSettings.CaminhoSistema}Impressos\CRONOGRAMA_COORDENADOR-{aprovado.sigla}.xlsx");

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        } 
    }

    private void OnCronogramaClienteClick(object sender, RoutedEventArgs e)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            var aprovado = cmbAprovados.SelectedItem as ProducaoAprovadoModel;

            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            // Abre o arquivo modelo
            FileStream inputStream = new(@$"{BaseSettings.CaminhoSistema}Modelos\CRONOGRAMA_CLIENTE.xlsx", FileMode.Open, FileAccess.Read);
            IWorkbook workbook = application.Workbooks.Open(inputStream);
            IWorksheet worksheet = workbook.Worksheets[0];
            // Preenche células fixas
            worksheet.Range["C1"].Text = @$"CRONOGRAMA DE MONTAGEM NATAL {BaseSettings.Database} {Environment.NewLine} {aprovado.nome} - {aprovado.sigla}";

            char coluna = 'E';
            int linhaInicial = 8; // Inserir a partir da linha 8
            foreach (var item in vm.CronogramaTotalGerais.Where(x => !x.status.Contains("TOTAL")))
            {
                var valores = new double?[]
                {
                    item.sn1, item.sn2, item.sn3, item.sn4, item.sn5, item.sn6, item.sn7, item.sn8,
                    item.sn9, item.sn10, item.sn11, item.sn12, item.sn13, item.sn14, item.sn15, item.sn16
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    var cell = worksheet.Range[$"{(char)(coluna + i)}{linhaInicial}"];
                    if (valores[i].HasValue)
                        cell.Number = valores[i].Value;
                    else
                        cell.Value = ""; // ou cell.Clear();
                }

                linhaInicial++;
            }

            linhaInicial = 7; // Inserir a partir da linha 7
            foreach (var item in vm.ViewCronogramas)
            {
                worksheet.Range[$"A{linhaInicial}"].Text = item.item;
                worksheet.Range[$"B{linhaInicial}"].Text = item.localitem;
                worksheet.Range[$"C{linhaInicial}"].Text = item.descricao;
                worksheet.Range[$"D{linhaInicial}"].Number = Convert.ToDouble(item.qtd);

                var valores = new double?[]
                {
                    item.n1, item.n2, item.n3, item.n4, item.n5, item.n6, item.n7, item.n8,
                    item.n9, item.n10, item.n11, item.n12, item.n13, item.n14, item.n15, item.n16
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    var cell = worksheet.Range[$"{(char)(coluna + i)}{linhaInicial}"];
                    if (valores[i].HasValue)
                        cell.Number = valores[i].Value;
                    else
                        cell.Value = ""; // ou cell.Clear();
                }

                worksheet.Range["U" + linhaInicial].Text = item.obs_cliente;
                linhaInicial++;
                worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
            }
            worksheet.DeleteRow(linhaInicial, 1);

            linhaInicial = 2; // Inserir a partir da linha 2
            foreach (var item in vm.NoitescronogPessoas.Where(x => x.qtd_pessoas > 0))
            {
                worksheet.Range["V" + linhaInicial].Text = item.funcao;
                worksheet.Range["W" + linhaInicial].Number = Convert.ToDouble(item.qtd_pessoas);

                linhaInicial++;
                //worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
            }
            worksheet.UsedRange.AutofitRows();
            // Salva como novo arquivo
            FileStream outputStream = new(@$"{BaseSettings.CaminhoSistema}Impressos\CRONOGRAMA_CLIENTE-{aprovado.sigla}.xlsx", FileMode.Create, FileAccess.Write);
            workbook.SaveAs(outputStream);

            workbook.Close();
            inputStream.Close();
            outputStream.Close();

            Process.Start("explorer", @$"{BaseSettings.CaminhoSistema}Impressos\CRONOGRAMA_CLIENTE-{aprovado.sigla}.xlsx");

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}

public partial class CronogramaViewModel : ObservableObject
{
    private Context _dbContext;
    private DataBaseSettings _dataBaseSettings;

    public CronogramaViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
        _dbContext = new Context();
        //LoadData();
        //NoitescronogPessoas = [];
    }

    [ObservableProperty]
    private ObservableCollection<ProducaoAprovadoModel> aprovados;

    [ObservableProperty]
    private ObservableCollection<ViewCronogramaModel> viewCronogramas;

    [ObservableProperty]
    private ObservableCollection<CronogramaTotalGeralDTO> cronogramaTotalGerais;

    [ObservableProperty]
    private ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel> noitescronogPessoas;

    [ObservableProperty]
    private ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel> noitescronogPessoasManutencao;

    [ObservableProperty]
    private ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel> noitesCronogPessoasManutencaoExtra;

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> operacionalFuncoes;

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> funcoes;

    public async Task<ObservableCollection<ProducaoAprovadoModel>> GetAprovadosAsync()
    {
        var result = await _dbContext.ProducaoAprovados
            .OrderBy(f => f.sigla_serv)
            .ToListAsync();
        return new ObservableCollection<ProducaoAprovadoModel>(result);
    }

    public async Task<int> AddCronoBySiglaAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        string sql = @"
            INSERT INTO operacional.tblnoitescronog (codfecha)
            SELECT comercial.proposta_view_fecha.cod_linha_qdfecha
            FROM comercial.proposta_view_fecha
            LEFT JOIN operacional.tblnoitescronog
            ON comercial.proposta_view_fecha.cod_linha_qdfecha = operacional.tblnoitescronog.codfecha
            WHERE operacional.tblnoitescronog.codfecha IS NULL
            AND comercial.proposta_view_fecha.sigla = @Sigla;
        ";

        return await connection.ExecuteAsync(sql, new { Sigla = sigla });
    }

    public async Task<ObservableCollection<ViewCronogramaModel>> GetViewCronogramasAsync(string sigla)
    {
        var result = await _dbContext.ViewCronogramas
            .OrderBy(f => f.item)
            .Where(f => f.sigla_completa == sigla)
            .ToListAsync();
        return new ObservableCollection<ViewCronogramaModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasAsync(string sigla)
    {
        var result = await _dbContext.OperacionalNoitescronogPessoas
            .OrderBy(f => f.funcao)
            .Where(f => f.sigla == sigla && f.fase == "MONTAGEM" && !f.funcao.Contains("BRINQUEDO"))
            .ToListAsync();
        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasManutencaoAsync(string sigla)
    {
        var result = await _dbContext.OperacionalNoitescronogPessoas
            .OrderBy(f => f.funcao)
            .Where(f => f.sigla == sigla && f.fase == "MANUTENÇÃO PROGRAMADA")
            .ToListAsync();
        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasManutencaoExtraAsync(string sigla)
    {
        var result = await _dbContext.OperacionalNoitescronogPessoas
            .OrderBy(f => f.funcao)
            .Where(f => f.sigla == sigla && f.fase == "EXTRA")
            .ToListAsync();
        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalFuncoesCronogramaModel>> GetFuncoesAsync()
    {
        var funcoes = await _dbContext.OperacionalFuncoesCronogramas
                .OrderBy(f => f.funcao)
                .ToListAsync();

        return new ObservableCollection<OperacionalFuncoesCronogramaModel>(funcoes);
    }

    public async Task AddOperacionalNoitescronogPessoasAsync(string sigla, string fase)
    {
        try
        {
            var funcoes = await _dbContext.OperacionalFuncoesCronogramas
                .OrderBy(f => f.funcao)
                .ToListAsync();

            var funcoesSigla = await _dbContext.OperacionalNoitescronogPessoas
                .Where(f => f.sigla == sigla)
                .OrderBy(f => f.funcao)
                .ToListAsync();

            var funcoesFaltantes = funcoes
                .Where(f => !funcoesSigla.Any(s => s.funcao == f.funcao))
                .ToList();

            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in funcoesFaltantes)
                    {
                        await _dbContext.OperacionalNoitescronogPessoas.AddAsync(new OperacionalNoitescronogPessoaFuncaoModel
                        {
                            sigla = sigla,
                            fase = fase,
                            funcao = item.funcao,
                            qtd_noites = 0,
                            qtd_pessoas = 0,
                        });
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (PostgresException)
                {
                    await transaction.RollbackAsync();
                    throw;// new Exception($"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}", pgEx);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;// new Exception($"Erro inesperado: {ex.Message}", ex);
                }
            });

            //return null; // sucesso
        }
        catch (Exception)
        {
            //return ex.Message; // retorna erro para quem chamou
            throw;
        }
    }

    public async Task<ObservableCollection<CronogramaTotalGeralDTO>> GetCronogramaTotalGeralAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT status, 
                   sn1, sn2, sn3, sn4, sn5, 
                   sn6, sn7, sn8, sn9, sn10, 
                   sn11, sn12, sn13, sn14, sn15, sn16
            FROM operacional.qry_cronograma_total_geral
            WHERE sigla_completa = @Sigla;
        ";

        var result = await connection.QueryAsync<CronogramaTotalGeralDTO>(sql, new { Sigla = sigla });
        return new ObservableCollection<CronogramaTotalGeralDTO>(result);
    }

    public async Task<bool> AtualizarPessoasNoiteFuncao(OperacionalNoitescronogPessoaFuncaoModel model)
    {
        using var db = new Context();
        var modelExistente = await db.OperacionalNoitescronogPessoas.FindAsync(model.id);
        if (modelExistente == null)
            await db.OperacionalNoitescronogPessoas.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
        return true;
    }

    

    public async Task<bool> AtualizarPessoasNoiteCronograma(ViewCronogramaModel model)
    {
        _dbContext = new Context();
        var noiteCronog = new OperacionalNoiteCronogModel
        {
            codfecha = model.codfecha,
            sigla = model.sigla,
            obs_coordenador = model.obs_coordenador,
            obs_cliente = model.obs_cliente,
            n1 = model.n1,
            n2 = model.n2,
            n3 = model.n3,
            n4 = model.n4,
            n5 = model.n5,
            n6 = model.n6,
            n7 = model.n7,
            n8 = model.n8,
            n9 = model.n9,
            n10 = model.n10,
            n11 = model.n11,
            n12 = model.n12,
            n13 = model.n13,
            n14 = model.n14,
            n15 = model.n15,
            n16 = model.n16,
            extra = string.Empty
        };
        var modelExistente = await _dbContext.OperacionalNoiteCronogs.FindAsync(model.codfecha);
        
        if (modelExistente == null)
            _dbContext.OperacionalNoiteCronogs.Add(noiteCronog);
        else
            _dbContext.Entry(modelExistente).CurrentValues.SetValues(noiteCronog);

        await _dbContext.SaveChangesAsync();
        return true;
    }

}
