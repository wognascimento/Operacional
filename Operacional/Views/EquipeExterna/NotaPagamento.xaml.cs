using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para NotaPagamento.xam
/// </summary>
public partial class NotaPagamento : UserControl
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;
    private bool _initialized = false;

    public NotaPagamento()
    {
        InitializeComponent();
        DataContext = new NotaPagamentoViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            vm.Descricoes = await vm.GetDescricoesAsync();
            vm.Empresas = await vm.GetEmpresasAsync();
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

    private async void Equipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            var equipe = e.AddedItems[0] as EquipeExternaEquipeDTO;
            vm.Siglas = await vm.GetSiglasEquipeAsync(equipe.id_equipe);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
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

    private async void Aprovado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            var relatorio = e.AddedItems.Count == 0 ? new() : e.AddedItems[0] as RelatorioPagamentoModel; //e.AddedItems[0] as RelatorioPagamentoModel;
            vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(relatorio.cod_relatorio);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            
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

    private void RadGridView_AddingNewDataItem(object sender, Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e)
    {
        var relatorio = cmbAprovado.SelectedItem as RelatorioPagamentoModel;
        e.NewObject = new RelatorioDetalheModel
        {
            codrelatorio = relatorio.cod_relatorio,
            empresa_nf = relatorio.equipe,
            id_equipe = relatorio.id_equipe,
            cliente = relatorio.sigla,
            inserido_em = DateTime.Now,
            inserido_por = BaseSettings.Username,
        };
    }

    private void Pagamentos_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        var item = e.Row.Item as RelatorioDetalheModel; // Substitua YourItemType pelo tipo do seu item
        if (item == null)
            return;

        if (item.envia_fluxo)
        {
            e.IsValid = false;
            MessageBox.Show("Este lançamento já foi enviado para o fluxo e não pode ser alterado.", "Edição bloqueada", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Suponha que "Nome" seja a propriedade da coluna que você quer validar
        if (string.IsNullOrWhiteSpace(item.tipo_detalhe))
        {
            e.IsValid = false; // Define a linha como inválida
        }
    }

    private void Pagamentos_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
    {
        if (e.Row?.Item is RelatorioDetalheModel { envia_fluxo: true })
        {
            e.Cancel = true;
            MessageBox.Show("Este lançamento já foi enviado para o fluxo e não pode ser alterado.", "Edição bloqueada", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void RadGridView_RowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        try
        {
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            if (e.Row.Item is RelatorioDetalheModel linha)
            {
                if (linha.envia_fluxo)
                    return;

                var relatorio = cmbAprovado.SelectedItem as RelatorioPagamentoModel;
                await vm.AtualizarPagamentoAsync(linha);
                await vm.AtualizarSaldosRelatorioAsync(relatorio.cod_relatorio);
                vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(relatorio.cod_relatorio);
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

    private async void OnSendFluxo(object sender, RoutedEventArgs e)
    {

        RadWindow radWindow = new()
        {
            Content = new NotaPagamentoResumo(((EquipeExternaEquipeDTO)cmbEquipe.SelectedItem).id_equipe),
            ResizeMode = ResizeMode.NoResize,
            Header = "Resumo de Pagamentos",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Owner = this // torna modal em relação à janela atual
        };
        radWindow.ShowDialog();

        /*
        NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
        DateTime hoje = DateTime.Today;
        DateTime doisDiasDepois = hoje.AddDays(-2);
        var relatorio = cmbAprovado.SelectedItem as RelatorioPagamentoModel;
        var itensFiltrados = vm.RelatorioDetalhes.Where(item => !item.envia_fluxo && item.data_pagto >= doisDiasDepois);
        if (itensFiltrados.Any())
        {
            MessageBoxResult result = MessageBox.Show($"Existem {itensFiltrados.Count()} itens a serem enviados para o fluxo.\nDeseja continuar?", "Enviar Fluxo", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await vm.InserirFluxoAsync(relatorio.id_equipe);
                    await vm.AtualizarEnviadoFluxoAsync(itensFiltrados.ToList());
                    vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(relatorio.cod_relatorio);
                    MessageBox.Show("Itens enviados para fluxo.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

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
        }
        else
        {
            MessageBox.Show("Todos os itens enviados ou com pagamento retroativo.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        */
    }

}

public partial class NotaPagamentoViewModel : ObservableObject
{
    private DataBaseSettings _dataBaseSettings;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeDTO> equipes;

    [ObservableProperty]
    private ObservableCollection<RelatorioDetalheModel> relatorioDetalhes;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaDescricaoServicoModel> descricoes;

    [ObservableProperty]
    private ObservableCollection<ComprasEmpresaModel> empresas;

    [ObservableProperty]
    private ObservableCollection<RelatorioPagamentoModel> siglas;

    [ObservableProperty]
    private ObservableCollection<string> tipos = ["IMPOSTOS", "SERVIÇOS", "DESPESAS"];

    public NotaPagamentoViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
    }

    public  async Task<ObservableCollection<EquipeExternaEquipeDTO>> GetEquipesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaEquipeDTO>(@"
            SELECT valores.id_equipe, equipe.equipe_e
            FROM equipe_externa.tblequipesext equipe
            JOIN equipe_externa.tbl_valores_previsao_equipe valores
              ON equipe.id = valores.id_equipe
            GROUP BY valores.id_equipe, equipe.equipe_e
            ORDER BY equipe.equipe_e;");

        return new ObservableCollection<EquipeExternaEquipeDTO>(result);
    }
    public  async Task<ObservableCollection<RelatorioPagamentoModel>> GetSiglasEquipeAsync(long id_equipe)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<RelatorioPagamentoModel>(@"
            SELECT *
            FROM equipe_externa.tblrelatorio_pagamento
            WHERE id_equipe = @id_equipe
              AND tipo = 'SERVIÇOS'
            ORDER BY sigla;",
            new { id_equipe });

        return new ObservableCollection<RelatorioPagamentoModel>(result);
    }

    public  async Task<ObservableCollection<RelatorioDetalheModel>> GetPagamentosEquipeBySiglaAsync(long codrelatorio)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<RelatorioDetalheModel>(@"
            SELECT *
            FROM equipe_externa.tbl_detalhes_relatorio
            WHERE codrelatorio = @codrelatorio
            ORDER BY descricao;",
            new { codrelatorio });

        return new ObservableCollection<RelatorioDetalheModel>(result);
    }

    public  async Task<ObservableCollection<EquipeExternaDescricaoServicoModel>> GetDescricoesAsync()
    {
        ObservableCollection<string> descricoes = ["ADIANTAMENTO ALIMENTAÇÃO","ADIANTAMENTO TRANSPORTE","PAGAMENTO DE ALIMENTAÇÃO","PAGAMENTO DE TRANSPORTE","PAGAMENTO DE IMPRESSÃO","PAGAMENTO DE MATERIAL"];
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaDescricaoServicoModel>(@"
            SELECT *
            FROM equipe_externa.tbl_descricao_servicos
            WHERE descricao <> ALL(@descricoes)
            ORDER BY descricao;",
            new { descricoes = descricoes.ToArray() });

        return new ObservableCollection<EquipeExternaDescricaoServicoModel>(result);
    }

    public  async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ComprasEmpresaModel>(
            @"SELECT *
              FROM compras.tblempresa
              ORDER BY abreviacao;");

        return new ObservableCollection<ComprasEmpresaModel>(result);
    }

    public async Task<bool> AtualizarPagamentoAsync(RelatorioDetalheModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.cod_detalhe_relatorio is null or <= 0)
        {
            model.cod_detalhe_relatorio = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tbl_detalhes_relatorio
                (codrelatorio, tipo_detalhe, valor_detalhe, data, enviado_fin, descricao,
                 data_pagto, numero_nf, empresa_nf, id_equipe, totvs, cod_servico_totvs,
                 empresa_pagadora, aprovado_por, data_aprovado, exportado, cancelado,
                 cliente, envia_fluxo, saldo, inserido_por, inserido_em, alterado_por,
                 alterado_em, enviado_fluxo_por, enviado_fluxo_em)
                VALUES
                (@codrelatorio, @tipo_detalhe, @valor_detalhe, @data, @enviado_fin, @descricao,
                 @data_pagto, @numero_nf, @empresa_nf, @id_equipe, @totvs, @cod_servico_totvs,
                 @empresa_pagadora, @aprovado_por, @data_aprovado, @exportado, @cancelado,
                 @cliente, @envia_fluxo, @saldo, @inserido_por, @inserido_em, @alterado_por,
                 @alterado_em, @enviado_fluxo_por, @enviado_fluxo_em)
                RETURNING cod_detalhe_relatorio;",
                model);

            return true;
        }

        await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_detalhes_relatorio
            SET codrelatorio = @codrelatorio,
                tipo_detalhe = @tipo_detalhe,
                valor_detalhe = @valor_detalhe,
                data = @data,
                enviado_fin = @enviado_fin,
                descricao = @descricao,
                data_pagto = @data_pagto,
                numero_nf = @numero_nf,
                empresa_nf = @empresa_nf,
                id_equipe = @id_equipe,
                totvs = @totvs,
                cod_servico_totvs = @cod_servico_totvs,
                empresa_pagadora = @empresa_pagadora,
                aprovado_por = @aprovado_por,
                data_aprovado = @data_aprovado,
                exportado = @exportado,
                cancelado = @cancelado,
                cliente = @cliente,
                envia_fluxo = @envia_fluxo,
                saldo = @saldo,
                inserido_por = @inserido_por,
                inserido_em = @inserido_em,
                alterado_por = @alterado_por,
                alterado_em = @alterado_em,
                enviado_fluxo_por = @enviado_fluxo_por,
                enviado_fluxo_em = @enviado_fluxo_em
            WHERE cod_detalhe_relatorio = @cod_detalhe_relatorio;",
            model);

        return true;
    }

    public async Task AtualizarSaldosRelatorioAsync(long codRelatorio)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.ExecuteAsync("SELECT equipe_externa.atualizar_saldo_relatorio(@CodRelatorio)",
                                      new { CodRelatorio = codRelatorio });
    }

    public async Task InserirFluxoAsync(long id_equipe)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.ExecuteAsync("SELECT equipe_externa.inserir_fluxo(@IdEquipe)",
                                      new { IdEquipe = id_equipe });
    }

    public async Task AtualizarEnviadoFluxoAsync(List<RelatorioDetalheModel> itens)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_detalhes_relatorio
            SET envia_fluxo = true,
                enviado_fluxo_em = @enviado_fluxo_em,
                enviado_fluxo_por = @enviado_fluxo_por
            WHERE cod_detalhe_relatorio = ANY(@ids);",
            new
            {
                ids = itens.Select(i => i.cod_detalhe_relatorio).Where(id => id is not null).ToArray(),
                enviado_fluxo_em = DateTime.Now,
                enviado_fluxo_por = _dataBaseSettings.Username
            });
    }


}
