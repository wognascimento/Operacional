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
        AddHandler(Telerik.Windows.Controls.RadGridView.BeginningEditEvent,
            new EventHandler<Telerik.Windows.Controls.GridViewBeginningEditRoutedEventArgs>(Pagamentos_BeginningEdit));
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
                    await vm.EnviarFluxoAsync(vm.Relatorio.id_equipe, itensFiltrados.ToList());
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
                    Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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

    

    private void Pagamentos_BeginningEdit(object sender, Telerik.Windows.Controls.GridViewBeginningEditRoutedEventArgs e)
    {
        if (e.Row?.Item is RelatorioDetalheModel { envia_fluxo: true }) e.Cancel = true;
    }

    private void Pagamentos_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.IsInEditMode != true) return;
        if (e.Row.Item is not RelatorioDetalheModel item) return;
        if (item.envia_fluxo || item.codrelatorio <= 0 || string.IsNullOrWhiteSpace(item.tipo_detalhe))
        {
            e.IsValid = false;
            e.ValidationResults.Add(new Telerik.Windows.Controls.GridViewCellValidationResult
            {
                PropertyName = nameof(item.tipo_detalhe),
                ErrorMessage = item.envia_fluxo ? "Lancamento enviado ao fluxo: edicao bloqueada." : "Informe o tipo e selecione um relatorio salvo."
            });
            return;
        }
        var vm = (NotaDespesaViewModel)DataContext;
        ValidatedGridSave.Save(sender, e, () => vm.AtualizarPagamentoAsync(item), async () =>
        {
            var detalhes = await vm.GetPagamentosEquipeBySiglaAsync(item.codrelatorio);
            if (vm.Relatorio?.cod_relatorio == item.codrelatorio)
                vm.RelatorioDetalhes = detalhes;
        });
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
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaEquipeDTO>(@"
            SELECT id AS id_equipe, equipe_e
            FROM equipe_externa.tblequipesext
            ORDER BY equipe_e;");

        return new ObservableCollection<EquipeExternaEquipeDTO>(result);
    }

    public async Task<ObservableCollection<EquipeExternaDescricaoServicoModel>> GetDescricoesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaDescricaoServicoModel>(@"
            SELECT *
            FROM equipe_externa.tbl_descricao_servicos
            ORDER BY descricao;");

        return new ObservableCollection<EquipeExternaDescricaoServicoModel>(result);
    }

    public async Task<ObservableCollection<ComprasEmpresaModel>> GetEmpresasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ComprasEmpresaModel>(
            @"SELECT *
              FROM compras.tblempresa
              ORDER BY abreviacao;");

        return new ObservableCollection<ComprasEmpresaModel>(result);
    }

    public async Task<ObservableCollection<string>> GetAprovadosAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<string>(@"
            SELECT sigla_serv
            FROM producao.t_aprovados
            WHERE sigla_serv IS NOT NULL
            GROUP BY sigla_serv
            ORDER BY sigla_serv;");

        return new ObservableCollection<string>(result);
    }

    
    public  async Task<ObservableCollection<string>> GetSiglasEquipeAsync(long id_equipe)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<string>(@"
            SELECT cliente
            FROM equipe_externa.tbl_valores_previsao_equipe
            WHERE id_equipe = @id_equipe
            GROUP BY cliente
            ORDER BY cliente;",
            new { id_equipe });

        return new ObservableCollection<string>(result);
    }

    public async Task AtualizarRelatorioPagamentoAsync(RelatorioPagamentoModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var modelExistente = await connection.QueryFirstOrDefaultAsync<RelatorioPagamentoModel>(@"
            SELECT *
            FROM equipe_externa.tblrelatorio_pagamento
            WHERE id_equipe = @id_equipe
              AND sigla = @sigla
              AND tipo = 'DESPESAS'
            LIMIT 1;",
            model);

        if (modelExistente is null)
        {
            model.cod_relatorio = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tblrelatorio_pagamento
                (id_equipe, equipe, data, valor_liberado, empresa_pagadora, tipo, sigla)
                VALUES
                (@id_equipe, @equipe, @data, @valor_liberado, @empresa_pagadora, @tipo, @sigla)
                RETURNING cod_relatorio;",
                model);
        }
        else
        {
            model = modelExistente;
        }

        Relatorio = model;
    }

    public Task<bool> AtualizarPagamentoAsync(RelatorioDetalheModel model)
    {
        return NotaEquipeRepository.SalvarAsync(model);
    }

    public async Task AtualizarSaldosRelatorioAsync(long codRelatorio)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.ExecuteAsync("SELECT equipe_externa.atualizar_saldo_relatorio(@CodRelatorio)",
                                      new { CodRelatorio = codRelatorio });
    }

    public async Task<ObservableCollection<RelatorioDetalheModel>> GetPagamentosEquipeBySiglaAsync(long codrelatorio)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<RelatorioDetalheModel>(@"
            SELECT *
            FROM equipe_externa.tbl_detalhes_relatorio
            WHERE codrelatorio = @codrelatorio
            ORDER BY cod_detalhe_relatorio;",
            new { codrelatorio });

        return new ObservableCollection<RelatorioDetalheModel>(result);
    }

    public async Task EnviarFluxoAsync(long idEquipe, List<RelatorioDetalheModel> itens)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Coordinate simultaneous send operations for this team.
        await connection.ExecuteAsync("SELECT pg_advisory_xact_lock(@idEquipe);", new { idEquipe }, transaction);
        var ids = itens.Where(i => i.cod_detalhe_relatorio > 0).Select(i => i.cod_detalhe_relatorio!.Value).Distinct().ToArray();
        var pendentes = (await connection.QueryAsync<long>(@"
            SELECT cod_detalhe_relatorio FROM equipe_externa.tbl_detalhes_relatorio
            WHERE cod_detalhe_relatorio = ANY(@ids) AND id_equipe = @idEquipe
              AND NOT COALESCE(envia_fluxo, false) FOR UPDATE;", new { ids, idEquipe }, transaction)).ToArray();
        if (pendentes.Length != ids.Length || ids.Length == 0)
            throw new InvalidOperationException("Os lancamentos foram alterados ou enviados por outro usuario. Recarregue a tela.");
        await connection.ExecuteAsync("SELECT equipe_externa.inserir_fluxo(@idEquipe);", new { idEquipe }, transaction);
        if (await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_detalhes_relatorio SET envia_fluxo = true,
                enviado_fluxo_em = @data, enviado_fluxo_por = @usuario
            WHERE cod_detalhe_relatorio = ANY(@ids) AND id_equipe = @idEquipe;",
            new { ids, idEquipe, data = DateTime.Now, usuario = _dataBaseSettings.Username }, transaction) != ids.Length)
            throw new InvalidOperationException("Nem todos os lancamentos puderam ser marcados como enviados.");
        await transaction.CommitAsync();
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
