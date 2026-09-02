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

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para CadastroOrcamento.xam
/// </summary>
public partial class CadastroOrcamento : UserControl
{
    private bool _initialized = false;

    public CadastroOrcamento()
    {
        InitializeComponent();
        DataContext = new CadastroOrcamentoViewModel();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroOrcamentoViewModel vm = (CadastroOrcamentoViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            //vm.Aprovados = await vm.GetAprovadosAsync();
            vm.SiglasCrono = await vm.GetSiglasCronoAsync();
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
            CadastroOrcamentoViewModel vm = (CadastroOrcamentoViewModel)DataContext;
            var equipe = e.AddedItems[0] as EquipeExternaEquipeModel;
            vm.EquipePrevisoes = await vm.GetEquipePrevisoesAsync(equipe.id);
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
        var equipe = this.cmbEquipe.SelectedItem as EquipeExternaEquipeModel;
        if (e.AddedItems.Count == 0)
            return;
        var sigla = e.AddedItems[0] as string;

        RadWindow.Confirm(new DialogParameters
        {
            Content = $"Deseja adicionar a sigla  {sigla}  para a equipe  {equipe.equipe_e}",
            Header = "Confirmação",
            Closed = ConfirmacaoFechada
        });
    }

    private async void ConfirmacaoFechada(object sender, WindowClosedEventArgs e)
    {
        if (e.DialogResult == true)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                CadastroOrcamentoViewModel vm = (CadastroOrcamentoViewModel)DataContext;
                var equipe = this.cmbEquipe.SelectedItem as EquipeExternaEquipeModel;
                var sigla = this.cmbAprovado.SelectedItem as string;

                await vm.AddEquipeExternaValoresPrevisaoEquipeAsync(sigla, equipe.id);
                vm.EquipePrevisoes = await vm.GetEquipePrevisoesAsync(equipe.id);

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
        else
        {
            RadWindow.Alert(new DialogParameters
            {
                Content = "Operação cancelada pelo usuário.",
                Header = "Informação"
            });

            this.cmbAprovado.SelectedItem = null;
        }
    }

    private async void RadGridView_RowValidated(object sender, GridViewRowValidatedEventArgs e)
    {
        if (e.Row.Item is EquipeExternaValoresPrevisaoEquipeModel linha)
        {
            try
            {
                var vm = (CadastroOrcamentoViewModel)DataContext;
                await vm.AtualizarEquipeExternaValoresPrevisaoEquipeAsync(linha);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }
    }

    private void OnFinalizaOrcamentoPagamentoClick(object sender, RoutedEventArgs e)
    {
        var equipe = this.cmbEquipe.SelectedItem as EquipeExternaEquipeModel;
        RadWindow.Confirm(new DialogParameters
        {
            Content = $"Deseja adicionar orçamento da equipe nos pagamentos?: {Environment.NewLine} {equipe.equipe_e}",
            Header = "Confirmação",
            Closed = ConfirmacaoOrcamento
        });
    }

    private async void ConfirmacaoOrcamento(object sender, WindowClosedEventArgs e)
    {
        if (e.DialogResult == true)
        {
            try
            {
                var vm = (CadastroOrcamentoViewModel)DataContext;
                var siglas = this.itensOrcamento.Items.Cast<EquipeExternaValoresPrevisaoEquipeModel>()
                    .OrderBy(f => f.cliente)
                    .GroupBy(f => f.cliente)
                    .Select(g => g.Key).ToList();

                var funcoes = vm.EquipePrevisoes //this.itensOrcamento.Items.Cast<EquipeExternaValoresPrevisaoEquipeModel>()
                    .OrderBy(f => f.funcao)
                    .GroupBy(f => f.funcao)
                    .Select(g => g.Key).ToList();

                await vm.FinalizarOrcamentoPagamentoAsync(
                    ((EquipeExternaEquipeModel)this.cmbEquipe.SelectedItem).id,
                    new ObservableCollection<string>(siglas),
                    new ObservableCollection<string>(funcoes));

                RadWindow.Alert(new DialogParameters
                {
                    Content = "Orçamento adicionado aos pagamentos com sucesso!",
                    Header = "Informação"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }
        else
        {
            RadWindow.Alert(new DialogParameters
            {
                Content = "Operação cancelada pelo usuário.",
                Header = "Informação"
            });
        }
    }
}

public partial class CadastroOrcamentoViewModel : ObservableObject
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeModel> equipes;

    [ObservableProperty]
    private ObservableCollection<ProducaoAprovadoModel> aprovados;

    [ObservableProperty]
    private ObservableCollection<string> siglasCrono;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel> equipePrevisoes;

    public async Task<ObservableCollection<EquipeExternaEquipeModel>> GetEquipesAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaEquipeModel>(
            @"SELECT *
              FROM equipe_externa.tblequipesext
              ORDER BY equipe_e;");
        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }

    public async Task<ObservableCollection<ProducaoAprovadoModel>> GetAprovadosAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<ProducaoAprovadoModel>(
            @"SELECT *
              FROM producao.t_aprovados
              ORDER BY sigla_serv;");
        return new ObservableCollection<ProducaoAprovadoModel>(result);
    }

    public async Task<ObservableCollection<string>> GetSiglasCronoAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<string>(
            @"SELECT sigla
              FROM operacional.tblnoitescronog_qtd_pessoa_funcao
              WHERE sigla IS NOT NULL
              GROUP BY sigla
              ORDER BY sigla;");
        return new ObservableCollection<string>(result);
    }

    public async Task<ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>> GetEquipePrevisoesAsync(long id_equipe)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<EquipeExternaValoresPrevisaoEquipeModel>(
            @"SELECT *
              FROM equipe_externa.tbl_valores_previsao_equipe
              WHERE id_equipe = @id_equipe
              ORDER BY cliente, fase;",
            new { id_equipe });
        return new ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>(result);
    }

    public async Task AddEquipeExternaValoresPrevisaoEquipeAsync(string sigla, long id_equipe)
    {
        try
        {
            using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var funcoes = (await connection.QueryAsync<OperacionalNoitescronogPessoaFuncaoModel>(
                @"SELECT *
                  FROM operacional.tblnoitescronog_qtd_pessoa_funcao
                  WHERE sigla = @sigla
                    AND COALESCE(qtd_pessoas, 0) > 0
                  ORDER BY fase, funcao;",
                new { sigla },
                transaction)).ToList();

            var funcoesSigla = (await connection.QueryAsync<EquipeExternaValoresPrevisaoEquipeModel>(
                @"SELECT *
                  FROM equipe_externa.tbl_valores_previsao_equipe
                  WHERE cliente = @sigla
                    AND id_equipe = @id_equipe
                  ORDER BY fase, funcao;",
                new { sigla, id_equipe },
                transaction)).ToList();

            var funcoesFaltantes = funcoes
                .Where(f => !funcoesSigla.Any(s => s.cliente == f.sigla && s.fase == f.fase && s.funcao == f.funcao))
                .ToList();

            try
            {
                foreach (var item in funcoesFaltantes)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO equipe_externa.tbl_valores_previsao_equipe
                        (id_equipe, cliente, fase, funcao, valor_ano_anterior,
                         valor_ano_atual, lanche, transporte, inserido_por, inserido_em)
                        VALUES
                        (@id_equipe, @cliente, @fase, @funcao, 0, 0, 0, 0, @inserido_por, @inserido_em);",
                        new
                        {
                            id_equipe,
                            cliente = sigla,
                            item.fase,
                            item.funcao,
                            inserido_por = BaseSettings.Username,
                            inserido_em = DateTime.Now
                        },
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> AtualizarEquipeExternaValoresPrevisaoEquipeAsync(EquipeExternaValoresPrevisaoEquipeModel model)
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        if (model.cod_valor_previsao <= 0)
        {
            model.cod_valor_previsao = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tbl_valores_previsao_equipe
                (id_equipe, equipe_e, cliente, fase, funcao, valor_ano_anterior,
                 valor_ano_atual, lanche, transporte, inserido_por, inserido_em,
                 alterado_por, data_altera)
                VALUES
                (@id_equipe, @equipe_e, @cliente, @fase, @funcao, @valor_ano_anterior,
                 @valor_ano_atual, @lanche, @transporte, @inserido_por, @inserido_em,
                 @alterado_por, @data_altera)
                RETURNING cod_valor_previsao;",
                model);

            return true;
        }

        await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_valores_previsao_equipe
            SET id_equipe = @id_equipe,
                equipe_e = @equipe_e,
                cliente = @cliente,
                fase = @fase,
                funcao = @funcao,
                valor_ano_anterior = @valor_ano_anterior,
                valor_ano_atual = @valor_ano_atual,
                lanche = @lanche,
                transporte = @transporte,
                inserido_por = @inserido_por,
                inserido_em = @inserido_em,
                alterado_por = @alterado_por,
                data_altera = @data_altera
            WHERE cod_valor_previsao = @cod_valor_previsao;",
            model);

        return true;
    }

    public async Task FinalizarOrcamentoPagamentoAsync(long id_equipe, ObservableCollection<string> siglas, ObservableCollection<string> funcoes)
    {

        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);

        string sql = @"
            SELECT
                sigla, qtd_pessoas, qtd_noites, equipe,
                fase, funcao, valor_ano_atual, valor_total,
                lanche, transporte, id_equipe, indice_pessoas_noite,
                razaosocial, vai_equipe
            FROM equipe_externa.qry_previsao_valores_cronograma
            WHERE sigla = ANY(@Siglas) AND funcao = ANY(@Funcoes) AND id_equipe = @Idequipe;
        ";

        var result = await connection.QueryAsync<PrevisaoValorCronogramaDTO>(sql, new { Idequipe = id_equipe, Siglas = siglas, Funcoes = funcoes });
        var totalSigla = result
            .OrderBy(f => f.sigla)
            .GroupBy(f => f.sigla)
            .Select(g => new ValorClienteOrcamentoDTO
            {
                sigla = g.Key,
                valor_total = Convert.ToDouble( g.Sum(s => s.valor_total) ),
            }).ToList();

        
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        foreach (var item in totalSigla)
        {
            var relatorio = await connection.QueryFirstOrDefaultAsync<RelatorioPagamentoModel>(
                @"SELECT *
                  FROM equipe_externa.tblrelatorio_pagamento
                  WHERE sigla = @sigla
                    AND id_equipe = @id_equipe
                  LIMIT 1;",
                new { item.sigla, id_equipe },
                transaction);

            if (relatorio != null)
            {
                relatorio.valor_liberado = item.valor_total;
                relatorio.data = DateTime.Now;
                await connection.ExecuteAsync(@"
                    UPDATE equipe_externa.tblrelatorio_pagamento
                    SET valor_liberado = @valor_liberado,
                        data = @data
                    WHERE cod_relatorio = @cod_relatorio;",
                    relatorio,
                    transaction);
            }
            else
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO equipe_externa.tblrelatorio_pagamento
                    (id_equipe, equipe, sigla, valor_liberado, data, empresa_pagadora, tipo)
                    VALUES
                    (@id_equipe, @equipe, @sigla, @valor_liberado, @data, @empresa_pagadora, @tipo);",
                    new
                    {
                        id_equipe,
                        equipe = result.FirstOrDefault()?.equipe,
                        sigla = item.sigla,
                        valor_liberado = item.valor_total,
                        data = DateTime.Now,
                        empresa_pagadora = "LOCAÇÃO",
                        tipo = "SERVIÇOS"
                    },
                    transaction);
            }
        }

        await transaction.CommitAsync();
    }
}
