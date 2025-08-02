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
/// Interação lógica para CadastroOrcamento.xam
/// </summary>
public partial class CadastroOrcamento : UserControl
{
    public CadastroOrcamento()
    {
        InitializeComponent();
        DataContext = new CadastroOrcamentoViewModel();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroOrcamentoViewModel vm = (CadastroOrcamentoViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
            //vm.Aprovados = await vm.GetAprovadosAsync();
            vm.SiglasCrono = await vm.GetSiglasCronoAsync();
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
        using var _db = new Context();
        var result = await _db.Equipes
            .OrderBy(f => f.equipe_e)
            .ToListAsync();
        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }

    public async Task<ObservableCollection<ProducaoAprovadoModel>> GetAprovadosAsync()
    {
        using var _db = new Context();
        var result = await _db.ProducaoAprovados
            .OrderBy(f => f.sigla_serv)
            .ToListAsync();
        return new ObservableCollection<ProducaoAprovadoModel>(result);
    }

    public async Task<ObservableCollection<string>> GetSiglasCronoAsync()
    {
        using var _db = new Context();
        var result = await _db.OperacionalNoitescronogPessoas
            .OrderBy(f => f.sigla)
            .GroupBy(f => f.sigla)
            .Select(g => g.Key)
            .ToListAsync();
        return new ObservableCollection<string>(result);
    }

    public async Task<ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>> GetEquipePrevisoesAsync(long id_equipe)
    {
        using var _db = new Context();
        var result = await _db.EquipePrevisoes
            .OrderBy(f => f.cliente)
            .ThenBy(f => f.fase)
            .Where(f => f.id_equipe == id_equipe)
            .ToListAsync();
        return new ObservableCollection<EquipeExternaValoresPrevisaoEquipeModel>(result);
    }

    public async Task AddEquipeExternaValoresPrevisaoEquipeAsync(string sigla, long id_equipe)
    {
        try
        {
            using var _db = new Context();
            var funcoes = await _db.OperacionalNoitescronogPessoas
                .Where(f => f.sigla == sigla && f.qtd_pessoas > 0)
                .OrderBy(f => f.fase)
                .ThenBy(f => f.funcao)
                .ToListAsync();

            var funcoesSigla = await _db.EquipePrevisoes
                .Where(f => f.cliente == sigla && f.id_equipe == id_equipe)
                .OrderBy(f => f.fase)
                .ThenBy(f => f.funcao)
                .ToListAsync();

            var funcoesFaltantes = funcoes
                .Where(f => !funcoesSigla.Any(s => s.cliente == f.sigla && s.fase == f.fase && s.funcao == f.funcao))
                .ToList();

            var executionStrategy = _db.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in funcoesFaltantes)
                    {
                        await _db.EquipePrevisoes.AddAsync(new EquipeExternaValoresPrevisaoEquipeModel
                        {
                            id_equipe = id_equipe,
                            cliente = sigla,
                            fase = item.fase,
                            funcao = item.funcao,
                            valor_ano_anterior = 0,
                            valor_ano_atual = 0,
                            lanche = 0,
                            transporte = 0,
                            inserido_por = Environment.UserName,
                            inserido_em = DateTime.Now,
                        });
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (PostgresException)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> AtualizarEquipeExternaValoresPrevisaoEquipeAsync(EquipeExternaValoresPrevisaoEquipeModel model)
    {
        using var db = new Context();
        var modelExistente = await db.EquipePrevisoes.FindAsync(model.cod_valor_previsao);
        if (modelExistente == null)
            await db.EquipePrevisoes.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
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

        
        using var db = new Context();
        foreach (var item in totalSigla)
        {
            var relatorio = await db.RelatorioPagamentos.Where(f => f.sigla == item.sigla && f.id_equipe == id_equipe).FirstOrDefaultAsync();
            if (relatorio != null)
            {
                relatorio.valor_liberado = item.valor_total;
                relatorio.data = DateTime.Now;
                db.Entry(relatorio).State = EntityState.Modified;
            }
            else
            {
                await db.RelatorioPagamentos.AddAsync(new RelatorioPagamentoModel
                {
                    id_equipe = id_equipe,
                    equipe = result.FirstOrDefault()?.equipe,
                    sigla = item.sigla,
                    valor_liberado = item.valor_total,
                    data = DateTime.Now,
                    empresa_pagadora = "LOCAÇÃO",
                    tipo = "SERVIÇOS",
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
