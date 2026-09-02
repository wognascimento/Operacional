using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            if (dataObject is null)
            {
                MessageBox.Show("Selecione um lançamento para enviar ao fluxo.", "Enviar Fluxo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

            var linha = await vm.GetLinhaFluxoAsync(fluxo.NumeroDocumento, fluxo.DataPagamento, fluxo.Cnpj);
            if (linha.HasValue)
            {
                await vm.MarcarResumoEnviadoFluxoAsync(dataObject);
                dataObject.linha_fluxo = linha.Value;
                radResumo.Rebind();

                MessageBox.Show($"Lançamento já existe no financeiro (linha_fluxo: {linha}). A origem foi marcada como enviada.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                return;
            }

            int idGerado = await vm.InsertFluxoAsync(fluxo);
            await vm.MarcarResumoEnviadoFluxoAsync(dataObject);
            dataObject.linha_fluxo = idGerado;
            radResumo.Rebind();

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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
        const string sql = @"
            SELECT
                empresa_nf AS equipe,
                numero_nf,
                data,
                data_pagto,
                empresa_pagadora,
                SUM(valor_detalhe) AS valor_detalhe,
                SUM(saldo) AS saldo,
                MAX(fluxo.linha_fluxo) AS linha_fluxo
            FROM equipe_externa.tbl_detalhes_relatorio detalhe
            LEFT JOIN equipe_externa.tblequipesext equipe
                ON equipe.equipe_e = detalhe.empresa_nf
            LEFT JOIN financeiro.fluxo fluxo
                ON fluxo.data_pagamento = detalhe.data_pagto
               AND fluxo.cnpj = equipe.cgc
               AND fluxo.numero_documento = detalhe.numero_nf
            WHERE detalhe.id_equipe = @id_equipe
            GROUP BY empresa_nf, numero_nf, data, data_pagto, empresa_pagadora
            ORDER BY empresa_nf, numero_nf;";

        await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<RelatorioResumoDTO>(sql, new { id_equipe });

        return new ObservableCollection<RelatorioResumoDTO>(result);
    }

    public async Task<ObservableCollection<RelatorioResumoDTO>> GetPagamentosEquipeResumoAsync()
    {
        const string sql = @"
            SELECT
                empresa_nf AS equipe,
                numero_nf,
                data,
                data_pagto,
                empresa_pagadora,
                SUM(valor_detalhe) AS valor_detalhe,
                SUM(saldo) AS saldo,
                MAX(fluxo.linha_fluxo) AS linha_fluxo
            FROM equipe_externa.tbl_detalhes_relatorio detalhe
            LEFT JOIN equipe_externa.tblequipesext equipe
                ON equipe.equipe_e = detalhe.empresa_nf
            LEFT JOIN financeiro.fluxo fluxo
                ON fluxo.data_pagamento = detalhe.data_pagto
               AND fluxo.cnpj = equipe.cgc
               AND fluxo.numero_documento = detalhe.numero_nf
            GROUP BY empresa_nf, numero_nf, data, data_pagto, empresa_pagadora
            ORDER BY empresa_nf, numero_nf;";

        await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var result = await connection.QueryAsync<RelatorioResumoDTO>(sql);

        return new ObservableCollection<RelatorioResumoDTO>(result);
    }

    public async Task<string> GetContaAsync(string empresa_pagadora)
    {
        await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<string>(
            @"SELECT conta
              FROM compras.tblempresa
              WHERE abreviacao = @empresa_pagadora
              LIMIT 1;",
            new { empresa_pagadora });
    }

    public async Task<EquipeExternaEquipeModel> GetEquipeAsync(string equipe)
    {
        await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<EquipeExternaEquipeModel>(
            @"SELECT *
              FROM equipe_externa.tblequipesext
              WHERE equipe_e = @equipe
              LIMIT 1;",
            new { equipe });
    }

    public async Task<int?> GetLinhaFluxoAsync(string numeroDocumento, DateTime dataPagamento, string cnpj, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT linha_fluxo
            FROM financeiro.fluxo
            WHERE numero_documento = @NumeroDocumento
              AND data_pagamento = @DataPagamento
              AND cnpj = @Cnpj
            LIMIT 1;
        ";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<int?>(sql, new
        {
            NumeroDocumento = numeroDocumento,
            DataPagamento = dataPagamento.Date,
            Cnpj = cnpj
        });
    }

    public async Task MarcarResumoEnviadoFluxoAsync(RelatorioResumoDTO resumo, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE equipe_externa.tbl_detalhes_relatorio
            SET envia_fluxo = true,
                enviado_fluxo_em = @enviado_fluxo_em,
                enviado_fluxo_por = @enviado_fluxo_por
            WHERE empresa_nf = @equipe
              AND numero_nf = @numero_nf
              AND data_pagto = @data_pagto
              AND empresa_pagadora = @empresa_pagadora;";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.ExecuteAsync(sql, new
        {
            resumo.equipe,
            resumo.numero_nf,
            data_pagto = resumo.data_pagto.Date,
            resumo.empresa_pagadora,
            enviado_fluxo_em = DateTime.Now,
            enviado_fluxo_por = BaseSettings.Username
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
