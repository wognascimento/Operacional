using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using System.Windows.Data;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

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

    private async void ComparacaoPrevisarLancamento_Loaded(object sender, System.Windows.RoutedEventArgs e)
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
}

public partial class ComparacaoPrevisarLancamentoViewModel : ObservableObject
{
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<ComparacaoPrevisaoLancamentoDTO> comparacoes;

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
}
