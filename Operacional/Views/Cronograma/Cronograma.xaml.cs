using CommunityToolkit.Mvvm.ComponentModel;
using ClosedXML.Excel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using Operacional.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private bool _initialized = false;

    public Cronograma()
    {
        InitializeComponent();
        DataContext = new CronogramaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            vm.Aprovados = await vm.GetAprovadosAsync();
            vm.Funcoes = await vm.GetFuncoesAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });

            // opcional: desvincular o event handler
            Loaded -= UserControl_Loaded;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            vm.ViewCronogramas = await vm.GetViewCronogramasSiglaAsync(aprovado.sigla_serv);
            vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralSiglaAsync(aprovado.sigla_serv);
            await vm.AddOperacionalNoitescronogPessoasAsync(aprovado.sigla_serv, "MONTAGEM");
            vm.NoitescronogPessoas = await vm.GetOperacionalNoitescronogPessoasAsync(aprovado.sigla_serv);
            vm.NoitescronogPessoasManutencao = await vm.GetOperacionalNoitescronogPessoasManutencaoAsync(aprovado.sigla_serv);
            vm.NoitesCronogPessoasManutencaoExtra = await vm.GetOperacionalNoitescronogPessoasManutencaoExtraAsync(aprovado.sigla_serv);
            vm.NoitesCronogPessoasDesmontagem = await vm.GetOperacionalNoitescronogPessoasDesmontagemAsync(aprovado.sigla_serv);
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            vm.ViewCronogramas = await vm.GetViewCronogramasCompletoAsync(aprovado.sigla);
            vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralCompletoAsync(aprovado.sigla);
            await vm.AddOperacionalNoitescronogPessoasAsync(aprovado.sigla, "MONTAGEM");
            vm.NoitescronogPessoas = await vm.GetOperacionalNoitescronogPessoasAsync(aprovado.sigla);
            vm.NoitescronogPessoasManutencao = await vm.GetOperacionalNoitescronogPessoasManutencaoAsync(aprovado.sigla);
            vm.NoitesCronogPessoasManutencaoExtra = await vm.GetOperacionalNoitescronogPessoasManutencaoExtraAsync(aprovado.sigla);
            vm.NoitesCronogPessoasDesmontagem = await vm.GetOperacionalNoitescronogPessoasDesmontagemAsync(aprovado.sigla);
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
        }
    }

    private static readonly string[] sourceArray = new[] { "EQUIPE EXTERNA", "COORD.+ASSIST.", "ELETRICISTA" };

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
                if (_tipoCronograma == "COMPLETO")
                    vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralCompletoAsync(sigla);
                else
                    vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralSiglaAsync(sigla);
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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

    private void RadGridView_AddingNewDataItemDesmont(object sender, GridViewAddingNewEventArgs e)
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
            fase = "DESMONTAGEM"
        };
    }

    private async void RadGridView_RowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        if (e.Row.Item is OperacionalNoitescronogPessoaFuncaoModel linha)
        {
            try
            {
                var vm = (CronogramaViewModel)DataContext;
                var aprovado = cmbAprovados.SelectedItem as ProducaoAprovadoModel;
                var sigla = _tipoCronograma == "COMPLETO" ? aprovado.sigla : aprovado.sigla_serv;
                await vm.AtualizarPessoasNoiteFuncao(linha);
                if (_tipoCronograma == "COMPLETO")
                    vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralCompletoAsync(sigla);
                else
                    vm.CronogramaTotalGerais = await vm.GetCronogramaTotalGeralSiglaAsync(sigla);
           
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
        GerarCronogramaExcel(
            "CRONOGRAMA_COORDENADOR.xlsx",
            "CRONOGRAMA_COORDENADOR",
            item => item.obs_coordenador,
            totais => totais.Where(x => sourceArray.Any(s => x.status.Contains(s))),
            preencherTotaisAposItens: true);
    }

    private void OnCronogramaClienteClick(object sender, RoutedEventArgs e)
    {
        GerarCronogramaExcel(
            "CRONOGRAMA_CLIENTE.xlsx",
            "CRONOGRAMA_CLIENTE",
            item => item.obs_cliente,
            totais => totais.Where(x => !x.status.Contains("TOTAL")),
            exibirTotais: false,
            ocultarNumerosNoites: true,
            ajustarAreaImpressaoAteItens: true);
    }

    private void GerarCronogramaExcel(
        string modelo,
        string prefixoArquivo,
        Func<ViewCronogramaModel, string?> obterObservacao,
        Func<IEnumerable<CronogramaTotalGeralDTO>, IEnumerable<CronogramaTotalGeralDTO>> filtrarTotais,
        bool preencherTotaisAposItens = false,
        bool exibirTotais = true,
        bool ocultarNumerosNoites = false,
        bool ajustarAreaImpressaoAteItens = false)
    {
        try
        {
            CronogramaViewModel vm = (CronogramaViewModel)DataContext;
            var aprovado = cmbAprovados.SelectedItem as ProducaoAprovadoModel;
            if (aprovado is null)
            {
                MessageBox.Show("Selecione um aprovado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var outputPath = SistemaPathResolver.GetImpressosPath($"{prefixoArquivo}-{aprovado.sigla}.xlsx");
            using var workbook = new XLWorkbook(SistemaPathResolver.GetModeloPath(modelo));
            var worksheet = workbook.Worksheet(1);

            worksheet.Cell("C1").Value = @$"CRONOGRAMA DE MONTAGEM NATAL {BaseSettings.Database} {Environment.NewLine} {aprovado.nome} - {aprovado.sigla}";

            const char coluna = 'E';
            var totais = filtrarTotais(vm.CronogramaTotalGerais).ToList();

            int linhaInicial = 8;
            if (exibirTotais && !preencherTotaisAposItens)
            {
                foreach (var item in totais)
                {
                    PreencherValoresNoites(worksheet, linhaInicial, coluna, item.sn1, item.sn2, item.sn3, item.sn4, item.sn5, item.sn6, item.sn7, item.sn8, item.sn9, item.sn10, item.sn11, item.sn12, item.sn13, item.sn14, item.sn15, item.sn16);
                    linhaInicial++;
                }
            }

            linhaInicial = 7;
            foreach (var item in vm.ViewCronogramas)
            {
                worksheet.Cell($"A{linhaInicial}").Value = item.item;
                worksheet.Cell($"B{linhaInicial}").Value = item.localitem;
                worksheet.Cell($"C{linhaInicial}").Value = item.descricao;
                worksheet.Cell($"D{linhaInicial}").Value = Convert.ToDouble(item.qtd);

                if (ocultarNumerosNoites)
                {
                    PreencherMarcacoesNoites(worksheet, linhaInicial, coluna, item.n1, item.n2, item.n3, item.n4, item.n5, item.n6, item.n7, item.n8, item.n9, item.n10, item.n11, item.n12, item.n13, item.n14, item.n15, item.n16);
                }
                else
                {
                    PreencherValoresNoites(worksheet, linhaInicial, coluna, item.n1, item.n2, item.n3, item.n4, item.n5, item.n6, item.n7, item.n8, item.n9, item.n10, item.n11, item.n12, item.n13, item.n14, item.n15, item.n16);
                }
                worksheet.Cell("U" + linhaInicial).Value = obterObservacao(item);

                linhaInicial++;
                worksheet.Row(linhaInicial).InsertRowsAbove(1);
            }
            worksheet.Row(linhaInicial).Delete();

            var ultimaLinhaItens = linhaInicial - 1;

            if (exibirTotais && preencherTotaisAposItens)
            {
                PreencherTotaisCronograma(worksheet, linhaInicial, totais);
            }

            linhaInicial = 2;
            foreach (var item in vm.NoitescronogPessoas.Where(x => x.qtd_pessoas > 0))
            {
                worksheet.Cell("V" + linhaInicial).Value = item.funcao;
                worksheet.Cell("W" + linhaInicial).Value = Convert.ToDouble(item.qtd_pessoas);
                linhaInicial++;
            }

            RemoverFormatacaoCondicionalNoites(worksheet);

            if (ajustarAreaImpressaoAteItens && ultimaLinhaItens >= 7)
            {
                worksheet.PageSetup.PrintAreas.Clear();
                worksheet.PageSetup.PrintAreas.Add(1, 1, ultimaLinhaItens, 21);
            }

            if (!preencherTotaisAposItens && !ajustarAreaImpressaoAteItens)
            {
                worksheet.Rows().AdjustToContents();
            }

            workbook.SaveAs(outputPath);
            SistemaPathResolver.OpenInExplorer(outputPath);
        }
        catch (Exception ex)
        {
            Operacional.ErrorDialog.Show(ex, "Erro");
        }
    }

    private static void RemoverFormatacaoCondicionalNoites(IXLWorksheet worksheet)
    {
        worksheet.ConditionalFormats.Remove(format =>
            format.Ranges.Any(range =>
                range.RangeAddress.FirstAddress.ColumnNumber <= 20 &&
                range.RangeAddress.LastAddress.ColumnNumber >= 5));
    }

    private static void PreencherValoresNoites(IXLWorksheet worksheet, int linha, char colunaInicial, params double?[] valores)
    {
        for (int i = 0; i < valores.Length; i++)
        {
            var cell = worksheet.Cell($"{(char)(colunaInicial + i)}{linha}");
            if (valores[i].HasValue)
            {
                cell.Value = valores[i].Value;
                AplicarFundoNoite(cell, valores[i]);
                continue;
            }

            cell.Value = string.Empty;
            AplicarFundoNoite(cell, null);
        }
    }

    private static void PreencherMarcacoesNoites(IXLWorksheet worksheet, int linha, char colunaInicial, params double?[] valores)
    {
        for (int i = 0; i < valores.Length; i++)
        {
            var cell = worksheet.Cell($"{(char)(colunaInicial + i)}{linha}");
            cell.Value = string.Empty;
            cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
            cell.Style.Fill.BackgroundColor = valores[i].HasValue
                ? XLColor.FromArgb(217, 217, 217)
                : XLColor.White;
        }
    }

    private static void AplicarFundoNoite(IXLCell cell, double? valor)
    {
        cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
        cell.Style.Fill.BackgroundColor = valor switch
        {
            null => XLColor.White,
            0 => XLColor.LightBlue,
            _ => XLColor.FromArgb(91, 155, 213)
        };
    }

    private static void PreencherTotaisCronograma(IXLWorksheet worksheet, int linhaInicial, IReadOnlyList<CronogramaTotalGeralDTO> totais)
    {
        const int totalColunaInicial = 5;
        const int totalColunasNoite = 16;

        for (int i = 0; i < totais.Count; i++)
        {
            var linha = linhaInicial + i;
            var item = totais[i];
            PreencherValoresNoites(worksheet, linha, 'E', item.sn1, item.sn2, item.sn3, item.sn4, item.sn5, item.sn6, item.sn7, item.sn8, item.sn9, item.sn10, item.sn11, item.sn12, item.sn13, item.sn14, item.sn15, item.sn16);
            AplicarFundoBrancoNoites(worksheet, linha);
        }

        var linhaTotal = linhaInicial + totais.Count;
        for (int coluna = totalColunaInicial; coluna < totalColunaInicial + totalColunasNoite; coluna++)
        {
            var letraColuna = XLHelper.GetColumnLetterFromNumber(coluna);
            var cell = worksheet.Cell(linhaTotal, coluna);
            cell.FormulaA1 = $"SUM({letraColuna}{linhaInicial}:{letraColuna}{linhaTotal - 1})";
            cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
            cell.Style.Fill.BackgroundColor = XLColor.White;
        }
    }

    private static void AplicarFundoBrancoNoites(IXLWorksheet worksheet, int linha)
    {
        for (int coluna = 5; coluna <= 20; coluna++)
        {
            var cell = worksheet.Cell(linha, coluna);
            cell.Style.Fill.PatternType = XLFillPatternValues.Solid;
            cell.Style.Fill.BackgroundColor = XLColor.White;
        }
    }

}

public partial class CronogramaViewModel : ObservableObject
{
    private DataBaseSettings _dataBaseSettings;

    public CronogramaViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
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
    private ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel> noitesCronogPessoasDesmontagem;

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> operacionalFuncoes;

    [ObservableProperty]
    private ObservableCollection<OperacionalFuncoesCronogramaModel> funcoes;

    public async Task<ObservableCollection<ProducaoAprovadoModel>> GetAprovadosAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ProducaoAprovadoModel>(
            @"SELECT *
              FROM producao.t_aprovados
              ORDER BY sigla_serv;");

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

    public async Task<ObservableCollection<ViewCronogramaModel>> GetViewCronogramasSiglaAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ViewCronogramaModel>(
            @"SELECT *
              FROM operacional.qry_cronograma
              WHERE sigla = @sigla
              ORDER BY item;",
            new { sigla });

        return new ObservableCollection<ViewCronogramaModel>(result);
    }

    public async Task<ObservableCollection<ViewCronogramaModel>> GetViewCronogramasCompletoAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ViewCronogramaModel>(
            @"SELECT *
              FROM operacional.qry_cronograma
              WHERE sigla_completa = @sigla
              ORDER BY item;",
            new { sigla });

        return new ObservableCollection<ViewCronogramaModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
            @"SELECT *
              FROM operacional.tblnoitescronog_qtd_pessoa_funcao
              WHERE sigla = @sigla
                AND fase = 'MONTAGEM'
                AND COALESCE(funcao, '') NOT ILIKE '%BRINQUEDO%'
              ORDER BY funcao;",
            new { sigla });

        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasManutencaoAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
            @"SELECT *
              FROM operacional.tblnoitescronog_qtd_pessoa_funcao
              WHERE sigla = @sigla
                AND fase = 'MANUTENÇÃO PROGRAMADA'
              ORDER BY funcao;",
            new { sigla });

        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasManutencaoExtraAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
            @"SELECT *
              FROM operacional.tblnoitescronog_qtd_pessoa_funcao
              WHERE sigla = @sigla
                AND fase = 'EXTRA'
              ORDER BY funcao;",
            new { sigla });

        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>> GetOperacionalNoitescronogPessoasDesmontagemAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
            @"SELECT *
              FROM operacional.tblnoitescronog_qtd_pessoa_funcao
              WHERE sigla = @sigla
                AND fase = 'DESMONTAGEM'
              ORDER BY funcao;",
            new { sigla });

        return new ObservableCollection<OperacionalNoitescronogPessoaFuncaoModel>(result);
    }

    public async Task<ObservableCollection<OperacionalFuncoesCronogramaModel>> GetFuncoesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var funcoes = await connection.QueryAsync<OperacionalFuncoesCronogramaModel>(
            @"SELECT *
              FROM operacional.tblfuncoes_cronograma
              ORDER BY funcao;");

        return new ObservableCollection<OperacionalFuncoesCronogramaModel>(funcoes);
    }

    public async Task AddOperacionalNoitescronogPessoasAsync(string sigla, string fase)
    {
        try
        {
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var funcoes = (await connection.QueryAsync<OperacionalFuncoesCronogramaModel>(
                @"SELECT *
                  FROM operacional.tblfuncoes_cronograma
                  ORDER BY funcao;",
                transaction: transaction)).ToList();

            var funcoesSigla = (await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
                @"SELECT *
                  FROM operacional.tblnoitescronog_qtd_pessoa_funcao
                  WHERE sigla = @sigla
                  ORDER BY funcao;",
                new { sigla },
                transaction)).ToList();

            var funcoesFaltantes = funcoes
                .Where(f => !funcoesSigla.Any(s => s.funcao == f.funcao))
                .ToList();

            try
            {
                foreach (var item in funcoesFaltantes)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO operacional.tblnoitescronog_qtd_pessoa_funcao
                        (sigla, fase, funcao, qtd_noites, qtd_pessoas)
                        VALUES (@sigla, @fase, @funcao, 0, 0);",
                        new { sigla, fase, item.funcao },
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            //return null; // sucesso
        }
        catch (Exception)
        {
            //return ex.Message; // retorna erro para quem chamou
            throw;
        }
    }

    public async Task<ObservableCollection<CronogramaTotalGeralDTO>> GetCronogramaTotalGeralSiglaAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT status, 
                   sn1, sn2, sn3, sn4, sn5, 
                   sn6, sn7, sn8, sn9, sn10, 
                   sn11, sn12, sn13, sn14, sn15, sn16
            FROM operacional.qry_cronograma_total_geral
            WHERE sigla = @Sigla;
        ";

        var result = await connection.QueryAsync<CronogramaTotalGeralDTO>(sql, new { Sigla = sigla });
        return new ObservableCollection<CronogramaTotalGeralDTO>(result);
    }

    public async Task<ObservableCollection<CronogramaTotalGeralDTO>> GetCronogramaTotalGeralCompletoAsync(string sigla)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT status, 
                   SUM(sn1) AS sn1, SUM(sn2) AS sn2, SUM(sn3) AS sn3, SUM(sn4) AS sn4, SUM(sn5) AS sn5, 
                   SUM(sn6) AS sn6, SUM(sn7) AS sn7, SUM(sn8) AS sn8, SUM(sn9) AS sn9, SUM(sn10) AS sn10, 
                   SUM(sn11) AS sn11, SUM(sn12) AS sn12, SUM(sn13) AS sn13, SUM(sn14) AS sn14, SUM(sn15) AS sn15, SUM(sn16) AS sn16
            FROM operacional.qry_cronograma_total_geral
            WHERE sigla_completa = @Sigla
            GROUP BY status;
        ";

        var result = await connection.QueryAsync<CronogramaTotalGeralDTO>(sql, new { Sigla = sigla });
        return new ObservableCollection<CronogramaTotalGeralDTO>(result);
    }

    public async Task<bool> AtualizarPessoasNoiteFuncao(OperacionalNoitescronogPessoaFuncaoModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO operacional.tblnoitescronog_qtd_pessoa_funcao
                (sigla, fase, funcao, qtd_pessoas, qtd_noites, equipe)
                VALUES (@sigla, @fase, @funcao, @qtd_pessoas, @qtd_noites, @equipe)
                RETURNING id;",
                model);

            return true;
        }

        await connection.ExecuteAsync(@"
            UPDATE operacional.tblnoitescronog_qtd_pessoa_funcao
            SET sigla = @sigla,
                fase = @fase,
                funcao = @funcao,
                qtd_pessoas = @qtd_pessoas,
                qtd_noites = @qtd_noites,
                equipe = @equipe
            WHERE id = @id;",
            model);

        return true;
    }

    

    public async Task<bool> AtualizarPessoasNoiteCronograma(ViewCronogramaModel model)
    {
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
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO operacional.tblnoitescronog
            (codfecha, sigla, obs_coordenador, obs_cliente, n1, n2, n3, n4,
             n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, extra)
            VALUES
            (@codfecha, @sigla, @obs_coordenador, @obs_cliente, @n1, @n2, @n3, @n4,
             @n5, @n6, @n7, @n8, @n9, @n10, @n11, @n12, @n13, @n14, @n15, @n16, @extra)
            ON CONFLICT (codfecha) DO UPDATE SET
                sigla = EXCLUDED.sigla,
                obs_coordenador = EXCLUDED.obs_coordenador,
                obs_cliente = EXCLUDED.obs_cliente,
                n1 = EXCLUDED.n1,
                n2 = EXCLUDED.n2,
                n3 = EXCLUDED.n3,
                n4 = EXCLUDED.n4,
                n5 = EXCLUDED.n5,
                n6 = EXCLUDED.n6,
                n7 = EXCLUDED.n7,
                n8 = EXCLUDED.n8,
                n9 = EXCLUDED.n9,
                n10 = EXCLUDED.n10,
                n11 = EXCLUDED.n11,
                n12 = EXCLUDED.n12,
                n13 = EXCLUDED.n13,
                n14 = EXCLUDED.n14,
                n15 = EXCLUDED.n15,
                n16 = EXCLUDED.n16,
                extra = EXCLUDED.extra;",
            noiteCronog);

        return true;
    }

}
