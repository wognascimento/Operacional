using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase.Models.DTOs.Api;
using System.Windows;
using Telerik.Windows.Controls;

namespace Operacional.Views.EquipeExterna.Consultas;

/// <summary>
/// Lógica interna para BuscarLancamentos.xaml
/// </summary>
public partial class BuscarLancamentos : RadWindow
{
    public BuscarLancamentos()
    {
        InitializeComponent();
        this.Loaded += BuscarLancamentos_Loaded;    
    }

    private async void BuscarLancamentos_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ComparacaoPrevisarLancamentoViewModel vm = (ComparacaoPrevisarLancamentoViewModel)DataContext;
            vm.IsBusy = true;
            await vm.GetEquipeUsuariosAsync();
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

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        ComparacaoPrevisarLancamentoViewModel vm = (ComparacaoPrevisarLancamentoViewModel)DataContext;
        vm.IsBusy = true;
        var url = $"https://rest-api.cipolatti.com.br/api/equipe-lancamentos/user/{vm.EquipeUsuario.aux}";
        var resultado = await vm.GetLancamentosWeb<EquipeLancamentoDto>(url);

        foreach (var item in resultado.Data)
            item.id_equipe = vm.EquipeUsuario.id_equipe;
        
        await vm.InsertBatchAsync(resultado.Data);
        //Console.WriteLine(resultado.Message);
        vm.IsBusy = false;
        vm.CloseAction?.Invoke(true);
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        ComparacaoPrevisarLancamentoViewModel vm = (ComparacaoPrevisarLancamentoViewModel)DataContext;
        vm.IsBusy = true;
        foreach (var user in vm.EquipeUsuarios)
        {
            var url = $"https://rest-api.cipolatti.com.br/api/equipe-lancamentos/user/{user.aux}";
            var resultado = await vm.GetLancamentosWeb<EquipeLancamentoDto>(url);

            foreach (var item in resultado.Data)
                item.id_equipe = user.id_equipe;

            await vm.InsertBatchAsync(resultado.Data);
        }
        vm.IsBusy = false;
        vm.CloseAction?.Invoke(true);
    }
}
