using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models.DTOs;
using Operacional.DataBase.Models.DTOs.Api;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Telerik.Windows.Controls;

namespace Operacional.Views.EquipeExterna.Consultas;

/// <summary>
/// Interação lógica para ComparacaoPrevisarLancamento.xam
/// </summary>
public partial class ComparacaoPrevisarLancamento : UserControl
{
    public ComparacaoPrevisarLancamento()
    {
        InitializeComponent();
        DataContext = new ComparacaoPrevisarLancamentoViewModel();
        this.Loaded += ComparacaoPrevisarLancamento_Loaded;
    }

    private async void ComparacaoPrevisarLancamento_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ComparacaoPrevisarLancamentoViewModel vm = (ComparacaoPrevisarLancamentoViewModel)DataContext;
            vm.IsBusy = true;
            await vm.GetComparacaoPrevistoRealizadoAsync();
            vm.IsBusy = false;
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

    private void RadGridView_AutoGeneratingColumn(object sender, Telerik.Windows.Controls.GridViewAutoGeneratingColumnEventArgs e)
    {
        // Cabeçalho mais legível
        if (e.Column.Header != null)
            e.Column.Header = e.Column.Header.ToString().Replace("_", " ").ToUpper();

        // tenta achar o nome da propriedade atrelada à coluna
        string propertyName = null;

        // 1) para GridViewDataColumn (mais comum com AutoGenerateColumns)
        if (e.Column is GridViewDataColumn dataColumn)
        {
            if (dataColumn.DataMemberBinding is Binding binding)
                propertyName = binding.Path?.Path;
        }

        // 2) para outros tipos (GridViewComboBoxColumn, etc.) pode ter DataMemberBinding também
        if (propertyName == null)
        {
            if (e.Column is GridViewBoundColumnBase boundBase && boundBase.DataMemberBinding is Binding bind2)
                propertyName = bind2.Path?.Path;
        }

        // 3) fallback: usa o header como nome (útil se o binding não estiver presente)
        if (string.IsNullOrEmpty(propertyName))
            propertyName = e.Column.Header?.ToString() ?? string.Empty;

        // agora aplica formatações por propertyName
        switch (propertyName)
        {
            case nameof(ComparacaoPrevisaoLancamentoDTO.noites_previstas):
            case nameof(ComparacaoPrevisaoLancamentoDTO.qtd_pessoas_prevista):
            case nameof(ComparacaoPrevisaoLancamentoDTO.noites_lancadas):
            case nameof(ComparacaoPrevisaoLancamentoDTO.qtd_pessoas_lancadas):
            case nameof(ComparacaoPrevisaoLancamentoDTO.noites_extras):
            case nameof(ComparacaoPrevisaoLancamentoDTO.qtd_pessoas_extra):
            case nameof(ComparacaoPrevisaoLancamentoDTO.noites_total):
            case nameof(ComparacaoPrevisaoLancamentoDTO.qtd_pessoas_total):
                e.Column.TextAlignment = TextAlignment.Right;
                if (e.Column is GridViewDataColumn colInt)
                    colInt.DataFormatString = "{0:N0}"; // inteiro
                break;

            case nameof(ComparacaoPrevisaoLancamentoDTO.valor_total_previsto):
            case nameof(ComparacaoPrevisaoLancamentoDTO.valor_total_lancada):
            case nameof(ComparacaoPrevisaoLancamentoDTO.valor_total_extra):
            case nameof(ComparacaoPrevisaoLancamentoDTO.valor_total_lancadas):
                e.Column.TextAlignment = TextAlignment.Right;
                if (e.Column is GridViewDataColumn colMoney)
                    colMoney.DataFormatString = "{0:C2}"; // monetário
                break;

            default:
                // ajuste padrão por tipo (strings à esquerda, números à direita)
                if (e.Column is GridViewDataColumn defaultCol)
                {
                    var propInfo = typeof(ComparacaoPrevisaoLancamentoDTO).GetProperty(propertyName);
                    if (propInfo != null)
                    {
                        var t = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                        if (t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(decimal) || t == typeof(float))
                        {
                            defaultCol.TextAlignment = TextAlignment.Right;
                            defaultCol.DataFormatString = "{0:N2}";
                        }
                        else
                        {
                            defaultCol.TextAlignment = TextAlignment.Left;
                        }
                    }
                }
                break;
        }

        // largura star por padrão
        e.Column.Width = new GridViewLength(1, GridViewLengthUnitType.Star);
    }

    private async void OnBuscarLancamentosClick(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        // recupera a janela que contém este UserControl (se houver)
        var ownerWindow = Window.GetWindow(this) ?? Application.Current.MainWindow;

        ComparacaoPrevisarLancamentoViewModel vm = (ComparacaoPrevisarLancamentoViewModel)DataContext;
        
        RadWindow dlg = new BuscarLancamentos
        {
            // opcional: passar ViewModel ou dados para a janela:
            DataContext = vm,
            Owner = ownerWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        vm.CloseAction = (result) =>
        {
            dlg.DialogResult = result;
            dlg.Close();
        };

        // ShowDialog() abre modalmente (bloqueia a janela owner)
        bool? resultado = dlg.ShowDialog();

        if (resultado == true)
        {
            try
            {
                vm.IsBusy = true;
                await vm.GetComparacaoPrevistoRealizadoAsync();
                vm.IsBusy = false;
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
        else
        {
            // usuário cancelou ou fechou
        }
    }
}

public partial class ComparacaoPrevisarLancamentoViewModel : ObservableObject
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<ComparacaoPrevisaoLancamentoDTO> comparacoes;

    [ObservableProperty]
    private ObservableCollection<EquipeUsuarioDTO> equipeUsuarios = [];

    [ObservableProperty]
    private EquipeUsuarioDTO? equipeUsuario;

    [ObservableProperty]
    public Action<bool?>? closeAction;

    public async Task GetComparacaoPrevistoRealizadoAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        string sql = @"
                    SELECT 
                        sigla, 
                        equipe_e, 
                        fase, 
                        funcoes, 
                        noites_previstas, 
                        qtd_pessoas_prevista, 
                        valor_total_previsto, 
                        noites_lancadas, 
                        qtd_pessoas_lancadas, 
                        valor_total_lancada, 
                        noites_extras, 
                        qtd_pessoas_extra, 
                        valor_total_extra,
                        noites_total, 
                        qtd_pessoas_total, 
                        valor_total_lancadas
	                FROM equipe_externa.qry_comparacao_previsao_lancamentos_new
                    ORDER BY sigla, equipe_e, fase, funcoes;
                ";
        Comparacoes = new ObservableCollection<ComparacaoPrevisaoLancamentoDTO>(await connection.QueryAsync<ComparacaoPrevisaoLancamentoDTO>(sql));
    }

    public async Task GetEquipeUsuariosAsync()
    {
        using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        string sql = @"
                    SELECT usuario.id_equipe, equipe.equipe_e, usuario.aux FROM equipe_externa.tblequipesext equipe
                    JOIN equipe_externa.tblusuario usuario ON equipe.id = usuario.id_equipe
                    WHERE usuario.aux IS NOT NULL AND equipe.equipe_e <> 'CIPOLATTI'
                    ORDER BY equipe.equipe_e;
                ";
        EquipeUsuarios = new ObservableCollection<EquipeUsuarioDTO>(await connection.QueryAsync<EquipeUsuarioDTO>(sql));
    }

    public async Task<ApiResponse<T>> GetLancamentosWeb<T>(string url, CancellationToken ct = default)
    {
        HttpClient _httpClient = new();
        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponse<T>>(stream, options, ct);
        return apiResponse ?? new ApiResponse<T> { Message = "Nenhuma resposta", Data = [] };
    }
    //
    public async Task InsertBatchAsync(List<EquipeLancamentoDto> items, CancellationToken ct = default)
    {
        const string upsertSql = @"
        INSERT INTO equipe_externa.tbl_presenca_equipe (
            id, id_equipe, id_aprovado, fase, funcao, data, pessoas, extra, created_at, updated_at
        ) VALUES (
            @id, @id_equipe, @id_aprovado, @fase, @funcao, @data, @pessoas, @extra, @created_at, @updated_at
        )
        ON CONFLICT (id) DO UPDATE
            SET
                id_equipe   = EXCLUDED.id_equipe,
                id_aprovado = EXCLUDED.id_aprovado,
                fase        = EXCLUDED.fase,
                funcao      = EXCLUDED.funcao,
                data        = EXCLUDED.data,
                pessoas     = EXCLUDED.pessoas,
                extra       = EXCLUDED.extra,
                updated_at  = EXCLUDED.updated_at
            RETURNING id;";

        const string deleteSql = @"
        DELETE FROM equipe_externa.tbl_presenca_equipe
        WHERE id = @id;";

        await using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync(ct);

        await using var tran = await conn.BeginTransactionAsync(ct);
        try
        {
            foreach (var item in items)
            {
                //await conn.ExecuteAsync(sql, item, transaction: tran);
                // se pessoas == 0 => delete
                if (item.pessoas == 0)
                {
                    await conn.ExecuteAsync(deleteSql, new { item.id }, transaction: tran);
                }
                else
                {
                    // upsert: retorna id (pode ser ignorado se não for usado)
                    await conn.QuerySingleAsync<int>(upsertSql, item, transaction: tran);
                }
            }
            await tran.CommitAsync(ct);
        }
        catch
        {
            await tran.RollbackAsync(ct);
            throw;
        }
    }


}
