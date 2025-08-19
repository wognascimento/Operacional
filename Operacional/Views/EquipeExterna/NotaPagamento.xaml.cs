using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para NotaPagamento.xam
/// </summary>
public partial class NotaPagamento : UserControl
{
    public NotaPagamento()
    {
        InitializeComponent();
        DataContext = new NotaPagamentoViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            vm.Descricoes = await vm.GetDescricoesAsync();
            vm.Empresas = await vm.GetEmpresasAsync();
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void Aprovado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            var relatorio = e.AddedItems[0] as RelatorioPagamentoModel;
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            inserido_por = Environment.UserName,
        };
    }

    private void Pagamentos_RowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        var item = e.Row.Item as RelatorioDetalheModel; // Substitua YourItemType pelo tipo do seu item
        if (item == null)
            return;

        // Suponha que "Nome" seja a propriedade da coluna que você quer validar
        if (string.IsNullOrWhiteSpace(item.tipo_detalhe))
        {
            e.IsValid = false; // Define a linha como inválida
        }
    }

    private async void RadGridView_RowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        try
        {
            NotaPagamentoViewModel vm = (NotaPagamentoViewModel)DataContext;
            if (e.Row.Item is RelatorioDetalheModel linha)
            {
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSendFluxo(object sender, RoutedEventArgs e)
    {
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
                    MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Todos os itens enviados ou com pagamento retroativo.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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
        using var context = new Context();
        var query = from equipe in context.Equipes
                    join valores in context.EquipePrevisoes
                    on equipe.id equals valores.id_equipe
                    group valores by new { valores.id_equipe, equipe.equipe_e } into g
                    select new EquipeExternaEquipeDTO
                    {
                        id_equipe = g.Key.id_equipe,
                        equipe_e = g.Key.equipe_e,
                        //TotalValorAnoAtual = g.Sum(v => v.valor_ano_atual)
                    };
        return new ObservableCollection<EquipeExternaEquipeDTO>(await query.ToListAsync());
    }
    /*
    public  async Task<ObservableCollection<string>> GetSiglasEquipeAsync(long id_equipe)
    {
        using var _db = new Context();
        var result = _db.EquipePrevisoes
            .Where(f => f.id_equipe == id_equipe)
            .OrderBy(f => f.cliente)
            .GroupBy(f => f.cliente)
            .Select(g => g.Key);
        return new ObservableCollection<string>(await result.ToListAsync());
    }
    */
    public  async Task<ObservableCollection<RelatorioPagamentoModel>> GetSiglasEquipeAsync(long id_equipe)
    {
        using var _db = new Context();
        var result = _db.RelatorioPagamentos
            .Where(f => f.id_equipe == id_equipe && f.tipo == "SERVIÇOS")
            .OrderBy(f => f.sigla);
        return new ObservableCollection<RelatorioPagamentoModel>(await result.ToListAsync());
    }

    public  async Task<ObservableCollection<RelatorioDetalheModel>> GetPagamentosEquipeBySiglaAsync(long codrelatorio)
    {
        using var _db = new Context();
        var result = await _db.RelatorioDetalhes.Where(f => f.codrelatorio == codrelatorio).OrderBy(f => f.descricao).ToListAsync();
        return new ObservableCollection<RelatorioDetalheModel>(result);
    }

    public  async Task<ObservableCollection<EquipeExternaDescricaoServicoModel>> GetDescricoesAsync()
    {
        using var _db = new Context();


        ObservableCollection<string> descricoes = ["ADIANTAMENTO ALIMENTAÇÃO","ADIANTAMENTO TRANSPORTE","PAGAMENTO DE ALIMENTAÇÃO","PAGAMENTO DE TRANSPORTE","PAGAMENTO DE IMPRESSÃO","PAGAMENTO DE MATERIAL"];
        var result = await _db.EquipeExternaDescricoes.Where(d => !descricoes.Contains(d.descricao)).OrderBy(f => f.descricao).ToListAsync();
        return new ObservableCollection<EquipeExternaDescricaoServicoModel>(result);
    }

    public  async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
    {
        using var _db = new Context();
        var result = await _db.ComprasEmpresas.OrderBy(f => f.abreviacao).ToListAsync();
        return new ObservableCollection<ComprasEmpresaModel>(result);
    }

    public async Task<bool> AtualizarPagamentoAsync(RelatorioDetalheModel model)
    {
        using var _db = new Context();
        var modelExistente = await _db.RelatorioDetalhes.FindAsync(model.cod_detalhe_relatorio);
        if (modelExistente == null)
            _db.RelatorioDetalhes.Add(model);
        else
            _db.Entry(modelExistente).CurrentValues.SetValues(model);
        await _db.SaveChangesAsync();
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
        using var _db = new Context();
        foreach (var item in itens)
        {
            var model = _db.RelatorioDetalhes.Find(item.cod_detalhe_relatorio);
            item.envia_fluxo = true;
            item.enviado_fluxo_em = DateTime.Now;
            item.enviado_fluxo_por = Environment.UserName;  
            _db.Entry(model).CurrentValues.SetValues(item);
        }  
        await _db.SaveChangesAsync();
    }


}
