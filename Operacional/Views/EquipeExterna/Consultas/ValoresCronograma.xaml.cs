using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Operacional.Views.EquipeExterna.Consultas;

/// <summary>
/// Interação lógica para ValoresCronograma.xam
/// </summary>
public partial class ValoresCronograma : UserControl
{
    public ValoresCronograma()
    {
        InitializeComponent();
        DataContext = new ValoresCronogramaViewModel();
        this.Loaded += ValoresCronograma_Loaded;
    }

    private async void ValoresCronograma_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ValoresCronogramaViewModel vm = (ValoresCronogramaViewModel)DataContext;
            vm.IsBusy = true;
            await vm.GetPrevisaoValoresCronogramaAsync();
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
}

public partial class ValoresCronogramaViewModel : ObservableObject
{

    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<PrevisaoValorCronogramaDTO> previsaoValores;

    [ObservableProperty]
    private bool isBusy;

    public async Task GetPrevisaoValoresCronogramaAsync()
    {
            using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            string sql = @"
                    SELECT  
                            sigla, qtd_pessoas, qtd_noites, equipe, 
                            fase, funcao, valor_ano_atual, valor_total, 
                            lanche, transporte, id_equipe, indice_pessoas_noite, 
                            razaosocial, vai_equipe
	                FROM equipe_externa.qry_previsao_valores_cronograma
                    ORDER BY sigla, equipe, fase, funcao;
                ";
            PrevisaoValores = new ObservableCollection<PrevisaoValorCronogramaDTO>(await connection.QueryAsync<PrevisaoValorCronogramaDTO>(sql)); 
        
    }
}
