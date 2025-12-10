using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para NotaPagamentoResumo.xam
/// </summary>
public partial class NotaPagamentoResumo : UserControl
{
    private long id_equipe;
    private DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public NotaPagamentoResumo(long id_equipe)
    {
        InitializeComponent();
        DataContext = new NotaPagamentoResumoViewModel();
        this.id_equipe = id_equipe;
        this.Loaded += NotaPagamentoResumo_Loaded;
    }

    private async void NotaPagamentoResumo_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            NotaPagamentoResumoViewModel vm = (NotaPagamentoResumoViewModel)DataContext;

            if (id_equipe == 0)
                vm.RelatorioResumo = await vm.GetPagamentosEquipeResumoAsync();
            else
                vm.RelatorioResumo = await vm.GetPagamentosEquipeResumoAsync(id_equipe);

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

    private async void OnEnviarFluxoClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            NotaPagamentoResumoViewModel vm = (NotaPagamentoResumoViewModel)DataContext;
            var selectedItem = radResumo.CurrentCellInfo.Item;
            var dataObject = selectedItem as RelatorioResumoDTO;
            var equipe = await vm.GetEquipeAsync(dataObject.equipe);

            var d = dataObject.data_pagto;
            var culture = new CultureInfo("pt-BR");

            // sem zero à esquerda (ex.: "10 – OUT")
            var semZero = $"{d.Month} – {d.ToString("MMM", culture).Replace(".", "").ToUpperInvariant()}";
            // com zero à esquerda (ex.: "10 – OUT", para maio seria "05 – MAI")
            var comZero = $"{d.ToString("MM")} – {d.ToString("MMM", culture).Replace(".", "").ToUpperInvariant()}";

            var fluxo = new FluxoDTO
            {
                Debito = (double)dataObject.valor_detalhe,
                DataEmissao = (DateTime)dataObject.data,
                NumeroDocumento = dataObject.numero_nf,
                DataVencimento = dataObject.data_pagto,
                DataPagamento = (DateTime)dataObject.data_pagto,
                Conta = await vm.GetContaAsync(dataObject.empresa_pagadora),
                Descricao = equipe.razaosocial,
                Depto = "RHE",
                Classif = "EQUIPE EXTERNA",
                Sub_Classif = "MOMADES",
                Class3 = $"N{BaseSettings.Database}",
                Tipo = "VAR",
                Razao_Social = equipe.razaosocial,
                Mes = comZero,
                Mes_Emissao = d.ToString("MM/yy", CultureInfo.InvariantCulture),
                Cnpj = equipe.cgc
            };

            var linha = await vm.GetLinhaFluxoAsync(fluxo.NumeroDocumento, fluxo.DataPagamento, fluxo.Conta, fluxo.Cnpj);
            if (!string.IsNullOrEmpty(linha))
            {
                MessageBox.Show($"Lançamento já existe no financeiro (linha_fluxo: {linha}).", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                return;
            }

            int idGerado = await vm.InsertFluxoAsync(fluxo);
            MessageBox.Show($"Lançamento inserido com sucesso! Id: {idGerado}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
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
}

public partial class NotaPagamentoResumoViewModel : ObservableObject
{
    private DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<RelatorioResumoDTO> relatorioResumo;

    public async Task<ObservableCollection<RelatorioResumoDTO>> GetPagamentosEquipeResumoAsync(long id_equipe)
    {
        using var _db = new Context();

        var result = await _db.RelatorioDetalhes
            .Where(f => f.id_equipe == id_equipe)
            .GroupBy(f => new 
            {
                f.empresa_nf,
                //f.tipo_detalhe,
                //f.descricao,
                f.numero_nf,
                f.data,
                f.data_pagto,
                f.empresa_pagadora
            })
            .Select(g => new RelatorioResumoDTO
            {
                equipe = g.Key.empresa_nf,
                //tipo_detalhe = g.Key.tipo_detalhe,
                //descricao = g.Key.descricao,
                numero_nf = g.Key.numero_nf,
                data = (DateTime)g.Key.data,
                data_pagto = (DateTime)g.Key.data_pagto,
                empresa_pagadora = g.Key.empresa_pagadora,
                valor_detalhe = g.Sum(x => x.valor_detalhe),
                saldo = g.Sum(x => x.saldo)
                // Se quiser manter outros campos (ex.: codrelatorio), pode usar g.Select(x => x.campo).FirstOrDefault()
            })
            .OrderBy(r => r.equipe)
            .ThenBy(r => r.numero_nf)
            .ToListAsync();

        return new ObservableCollection<RelatorioResumoDTO>(result);
    }

    public async Task<ObservableCollection<RelatorioResumoDTO>> GetPagamentosEquipeResumoAsync()
    {
        using var _db = new Context();

        var result = await _db.RelatorioDetalhes
            .GroupBy(f => new
            {
                f.empresa_nf,
                //f.tipo_detalhe,
                //f.descricao,
                f.numero_nf,
                f.data,
                f.data_pagto,
                f.empresa_pagadora
            })
            .Select(g => new RelatorioResumoDTO
            {
                equipe = g.Key.empresa_nf,
                //tipo_detalhe = g.Key.tipo_detalhe,
                //descricao = g.Key.descricao,
                numero_nf = g.Key.numero_nf,
                data = (DateTime)g.Key.data,
                data_pagto = (DateTime)g.Key.data_pagto,
                empresa_pagadora = g.Key.empresa_pagadora,
                valor_detalhe = g.Sum(x => x.valor_detalhe),
                saldo = g.Sum(x => x.saldo)
                // Se quiser manter outros campos (ex.: codrelatorio), pode usar g.Select(x => x.campo).FirstOrDefault()
            })
            .OrderBy(r => r.equipe)
            .ThenBy(r => r.numero_nf)
            .ToListAsync();

        return new ObservableCollection<RelatorioResumoDTO>(result);
    }

    public async Task<string> GetContaAsync(string empresa_pagadora)
    {
        using var _db = new Context();
        var conta = await _db.ComprasEmpresas
            .Where(e => e.abreviacao == empresa_pagadora)
            .Select(e => e.conta)
            .FirstOrDefaultAsync();
        return conta;
    }

    public async Task<EquipeExternaEquipeModel> GetEquipeAsync(string equipe)
    {
        using var _db = new Context();
        var razaosocial = await _db.Equipes
            .FirstOrDefaultAsync(e => e.equipe_e == equipe);
        return razaosocial;
    }

    public async Task<string?> GetLinhaFluxoAsync(string numeroDocumento, DateTime dataPagamento, string conta, string cnpj, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT linha_fluxo
            FROM financeiro.fluxo
            WHERE numero_documento = @NumeroDocumento
              AND data_pagamento = @DataPagamento
              AND conta = @Conta
              AND cnpj = @Cnpj
            LIMIT 1;
        ";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<string>(sql, new
        {
            NumeroDocumento = numeroDocumento,
            DataPagamento = dataPagamento,   // se você quer comparar com hora também
            Conta = conta,
            Cnpj = cnpj
        });
    }

    public async Task<int> InsertFluxoAsync(FluxoDTO fluxo, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO financeiro.fluxo (
                debito, data_emissao, numero_documento, data_vencimento, conta, descricao,
                data_pagamento, depto, classif, tipo, sub_classif, class3,
                razao_social, mes, mes_emissao, cnpj
            )
            VALUES (
                @Debito, @DataEmissao, @NumeroDocumento, @DataVencimento, @Conta, @Descricao,
                @DataPagamento, @Depto, @Classif, @Tipo, @Sub_Classif, @Class3,
                @Razao_Social, @Mes, @Mes_Emissao, @Cnpj
            )
            RETURNING linha_fluxo;
        ";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        // Inicia transação
        await using var tran = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            // Dapper mapeará as propriedades do objeto fluxo para os parâmetros @...
            var newId = await conn.QuerySingleAsync<int>(sql, fluxo, transaction: tran);
            await tran.CommitAsync(cancellationToken);
            return newId;
        }
        catch
        {
            await tran.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
