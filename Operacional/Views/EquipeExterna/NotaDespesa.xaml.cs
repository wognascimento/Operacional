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

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para NotaDespesa.xam
/// </summary>
public partial class NotaDespesa : UserControl
{

    DataBaseSettings BaseSettings = DataBaseSettings.Instance;
    private bool _initialized = false;

    public NotaDespesa()
    {
        InitializeComponent();
        DataContext = new NotaDespesaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaDespesaViewModel vm = (NotaDespesaViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            vm.Descricoes = await vm.GetDescricoesAsync();
            vm.Empresas = await vm.GetEmpresasAsync();
            vm.Siglas = await vm.GetAprovadosAsync();
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void Aprovado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaDespesaViewModel vm = (NotaDespesaViewModel)DataContext;

            var equipe = cmbEquipe.SelectedItem as EquipeExternaEquipeDTO;
            var sigla = e.AddedItems[0] as string;
            await vm.AtualizarRelatorioPagamentoAsync(
                new RelatorioPagamentoModel 
                {
                    data = DateTime.Now,
                    equipe = equipe.equipe_e,
                    id_equipe = equipe.id_equipe,
                    tipo = "DESPESAS",
                    sigla = sigla,
                    empresa_pagadora = "LOCAÇÃO",
                    valor_liberado = 0,

                });

            vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(vm.Relatorio.cod_relatorio);

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

    private async void OnSendFluxo(object sender, RoutedEventArgs e)
    {
        NotaDespesaViewModel vm = (NotaDespesaViewModel)DataContext;
        DateTime hoje = DateTime.Today;
        DateTime doisDiasDepois = hoje.AddDays(-2);
        //var relatorio = cmbAprovado.SelectedItem as RelatorioPagamentoModel;
        var itensFiltrados = vm.RelatorioDetalhes.Where(item => !item.envia_fluxo && item.data_pagto >= doisDiasDepois);
        if (itensFiltrados.Any())
        {
            MessageBoxResult result = MessageBox.Show($"Existem {itensFiltrados.Count()} itens a serem enviados para o fluxo.\nDeseja continuar?", "Enviar Fluxo", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await vm.InserirFluxoAsync(vm.Relatorio.id_equipe);
                    await vm.AtualizarEnviadoFluxoAsync(itensFiltrados.ToList());
                    vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(vm.Relatorio.cod_relatorio);
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

    private void RadGridView_AddingNewDataItem(object sender, Telerik.Windows.Controls.GridView.GridViewAddingNewEventArgs e)
    {
        NotaDespesaViewModel vm = (NotaDespesaViewModel)DataContext;
        e.NewObject = new RelatorioDetalheModel
        {
            codrelatorio = vm.Relatorio.cod_relatorio,
            empresa_nf = vm.Relatorio.equipe,
            id_equipe = vm.Relatorio.id_equipe,
            cliente = vm.Relatorio.sigla,
            inserido_em = DateTime.Now,
            inserido_por = BaseSettings.Username,
        };
    }

    private async void RadGridView_RowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            NotaDespesaViewModel vm = (NotaDespesaViewModel)DataContext;
            if (e.Row.Item is RelatorioDetalheModel linha)
            {
                await vm.AtualizarPagamentoAsync(linha);
                await vm.AtualizarSaldosRelatorioAsync(vm.Relatorio.cod_relatorio);
                vm.RelatorioDetalhes = await vm.GetPagamentosEquipeBySiglaAsync(vm.Relatorio.cod_relatorio);
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

    private void Pagamentos_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
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
}

public partial class NotaDespesaViewModel : ObservableObject
{
    private DataBaseSettings _dataBaseSettings;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeDTO> equipes;

    [ObservableProperty]
    private ObservableCollection<RelatorioDetalheModel> relatorioDetalhes;

    [ObservableProperty]
    private RelatorioPagamentoModel relatorio;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaDescricaoServicoModel> descricoes;

    [ObservableProperty]
    private ObservableCollection<ComprasEmpresaModel> empresas;

    [ObservableProperty]
    private ObservableCollection<string> siglas;

    [ObservableProperty]
    private ObservableCollection<string> tipos = ["IMPOSTOS", "SERVIÇOS", "DESPESAS"];

    public NotaDespesaViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
    }

    public async Task<ObservableCollection<EquipeExternaEquipeDTO>> GetEquipesAsync()
    {
        using var context = new Context();
        var query = from equipe in context.Equipes
                    orderby equipe.equipe_e
                    select new EquipeExternaEquipeDTO
                    {
                        id_equipe = equipe.id,
                        equipe_e = equipe.equipe_e,
                        //TotalValorAnoAtual = g.Sum(v => v.valor_ano_atual)
                    };
        return new ObservableCollection<EquipeExternaEquipeDTO>(await query.ToListAsync());
    }

    public async Task<ObservableCollection<EquipeExternaDescricaoServicoModel>> GetDescricoesAsync()
    {
        using var _db = new Context();

        ObservableCollection<string> descricoes = ["ADIANTAMENTO ALIMENTAÇÃO", "PAGAMENTO DE ALIMENTAÇÃO", "PAGAMENTO DE TRANSPORTE", "PAGAMENTO DE IMPRESSÃO", "PAGAMENTO DE MATERIAL", "ADIANTAMENTO TRANSPORTE"];
        
        var result = await _db.EquipeExternaDescricoes.OrderBy(f => f.descricao).ToListAsync();
        //var result = await _db.EquipeExternaDescricoes.Where(d => descricoes.Contains(d.descricao)).OrderBy(f => f.descricao).ToListAsync();
        return new ObservableCollection<EquipeExternaDescricaoServicoModel>(result);
    }

    public async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
    {
        using var _db = new Context();
        var result = await _db.ComprasEmpresas.OrderBy(f => f.abreviacao).ToListAsync();
        return new ObservableCollection<ComprasEmpresaModel>(result);
    }

    public async Task<ObservableCollection<string>> GetAprovadosAsync()
    {
        using var _db = new Context();
        var result = _db.ProducaoAprovados
            .GroupBy(f => f.sigla_serv)
            .Select(g => g.Key)
            .OrderBy(f => f);

        return new ObservableCollection<string>(result);
    }

    
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

    public async Task AtualizarRelatorioPagamentoAsync(RelatorioPagamentoModel model)
    {
        using var _db = new Context();
        var modelExistente = await _db.RelatorioPagamentos.FirstOrDefaultAsync(m => m.id_equipe == model.id_equipe && m.sigla == model.sigla && m.tipo == "DESPESAS");
        if (modelExistente == null)
            _db.RelatorioPagamentos.Add(model);
        else
           model = modelExistente;
        await _db.SaveChangesAsync();

        Relatorio = model;
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

    public async Task<ObservableCollection<RelatorioDetalheModel>> GetPagamentosEquipeBySiglaAsync(long codrelatorio)
    {
        using var _db = new Context();
        var result = await _db.RelatorioDetalhes.Where(f => f.codrelatorio == codrelatorio).OrderBy(f => f.cod_detalhe_relatorio).ToListAsync();
        return new ObservableCollection<RelatorioDetalheModel>(result);
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
            item.enviado_fluxo_por = _dataBaseSettings.Username;
            _db.Entry(model).CurrentValues.SetValues(item);
        }
        await _db.SaveChangesAsync();
    }


}