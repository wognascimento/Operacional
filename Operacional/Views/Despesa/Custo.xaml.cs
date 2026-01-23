using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.Views.Cronograma;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using static CustoModel;

namespace Operacional.Views.Despesa;

/// <summary>
/// Interação lógica para Custo.xam
/// </summary>
public partial class Custo : UserControl
{
    public Custo()
    {
        InitializeComponent();
        DataContext = new CustoViewModel();
        Loaded += Custo_Loaded;
    }

    private async void Custo_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CustoViewModel vm = (CustoViewModel)DataContext;
            await vm.GetCustosAsync();
            await vm.GetSiglasAsync();
            await vm.GetEtapasAsync();
            await vm.GetClassificacoesAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (PostgresException ex)
        {
            // Handle specific PostgreSQL exceptions here
            MessageBox.Show($"PostgreSQL Error ({ex.SqlState}): {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbException ex)
        {
            // Handle general database exceptions
            MessageBox.Show($"Database Error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void Classificacao_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not RadComboBox combo)
            return;

        if (combo.DataContext is not CustoModel item)
            return;

        if (string.IsNullOrWhiteSpace(item.Classificacao))
            return;

        if (DataContext is not CustoViewModel vm)
            return;

        item.Descricoes = await vm.GetDescricoesComCacheAsync(item.Classificacao);
    }


    private async void radGridViewRowValidating(object sender, GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.Item is not CustoModel item)
            return;

        // chama validação corretamente
        item.Validate();

        if (item.HasErrors)
        {
            e.IsValid = false;
            return;
        }

        if (DataContext is not CustoViewModel vm)
            return;

        try
        {
            if (item.codcusto == 0)
            {
                item.MarkAsAdded();
            }

            await vm.SalvarCustoAsync(item);
        }
        catch (Exception ex)
        {
            e.IsValid = false;
            MessageBox.Show(
                ex.Message,
                "Erro ao salvar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }   
}

public partial class CustoViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;
    private readonly Dictionary<string, ObservableCollection<string>> _descricoesCache = [];

    [ObservableProperty]
    private ObservableCollection<CustoModel> custos;

    [ObservableProperty]
    private ObservableCollection<string> siglas;

    [ObservableProperty]
    private ObservableCollection<string> etapas;

    [ObservableProperty]
    private ObservableCollection<string> classificacoes;
    /*
    [ObservableProperty]
    private ObservableCollection<string> descricoes;
    */

    public async Task GetCustosAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"SELECT * FROM operacional.t_custos;";
        var result = await connection.QueryAsync<CustoModel>(sql);

        var lista = new ObservableCollection<CustoModel>(result);

        foreach (var item in lista)
        {
            item.MarkAsLoaded();

            if (string.IsNullOrWhiteSpace(item.Classificacao))
                continue;

            if (!_descricoesCache.TryGetValue(item.Classificacao, out var descricoes))
            {
                descricoes = await GetDescricoesAsync(item.Classificacao);
                _descricoesCache[item.Classificacao] = descricoes;
            }

            item.Descricoes = descricoes;
        }

        Custos = lista;
    }

    public async Task<ObservableCollection<string>> GetDescricoesComCacheAsync(string classificacao)
    {
        if (_descricoesCache.TryGetValue(classificacao, out var cache))
            return cache;

        var descricoes = await GetDescricoesAsync(classificacao);
        _descricoesCache[classificacao] = descricoes;
        return descricoes;
    }

    public async Task GetSiglasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT
                sigla
            FROM
                comercial.clientes
            GROUP BY
                sigla
            ORDER BY
                sigla;
        ";

        var result = await connection.QueryAsync<string>(sql);
        Siglas = new ObservableCollection<string>(result);
    }

    public async Task GetEtapasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT
                fase
            FROM
                operacional.tblfases
            ORDER BY
                fase;
        ";

        var result = await connection.QueryAsync<string>(sql);
        Etapas = new ObservableCollection<string>(result);
    }

    public async Task GetClassificacoesAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT
                Tipo
            FROM
                operacional.tblbasecustos
            GROUP BY
                Tipo
            ORDER BY
                Tipo;
        ";

        var result = await connection.QueryAsync<string>(sql);
        Classificacoes = new ObservableCollection<string>(result);
    }

    public async Task<ObservableCollection<string>> GetDescricoesAsync(string classificacao)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql = @"
            SELECT 
                descr
            FROM 
                operacional.tblbasecustos
            WHERE 
                tipo = @classificacao
            GROUP BY 
                descr
            ORDER BY 
                descr;
        ";

        var result = await connection.QueryAsync<string>(sql, new { classificacao });
        return new ObservableCollection<string>(result);
    }

    public async Task SalvarCustoAsync(CustoModel item)
    {
        if (item.State == EntityState.Unchanged)
            return; // 🔴 NÃO salva dados carregados

        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        string sql;

        if (item.State == EntityState.Added) //if (item.codcusto == 0)
        {
            item.cadastro_por = _dataBaseSettings.Username;
            item.cadastro_data = DateTime.Now;

            sql = @"
                INSERT INTO operacional.t_custos
                (sigla, data, qtd, unid, etapa, classificacao, descricao, vunit, cadastro_por, cadastro_data)
                VALUES
                (@sigla, @data, @qtd, @unid, @etapa, @classificacao, @descricao, @vunit, @cadastro_por, @cadastro_data)
                RETURNING codcusto;
            ";

            item.codcusto = await connection.ExecuteScalarAsync<int>(sql, item);
        }
        else if (item.State == EntityState.Modified)
        {
            item.alterado_por = _dataBaseSettings.Username;
            item.alterado_data = DateTime.Now;

            sql = @"
                UPDATE operacional.t_custos
                SET
                    sigla = @sigla,
                    data = @data,
                    qtd = @qtd,
                    unid = @unid,
                    etapa = @etapa,
                    classificacao = @classificacao,
                    descricao = @descricao,
                    vunit = @vunit,
                    alterado_por = @alterado_por,
                    alterado_data = @alterado_data
                WHERE codcusto = @codcusto;
            ";

            await connection.ExecuteAsync(sql, item);
        }

        item.MarkAsSaved();
    }

}
