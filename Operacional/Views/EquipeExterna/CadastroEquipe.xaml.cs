using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para CadastroEquipe.xam
/// </summary>
public partial class CadastroEquipe : UserControl
{
    public CadastroEquipe()
    {
        InitializeComponent();
        DataContext = new CadastroEquipeViewModel();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroEquipeViewModel vm = (CadastroEquipeViewModel)DataContext;
            vm.Equipes = await vm.GetEquipesAsync();
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

    private async void EquipeRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroEquipeViewModel vm = (CadastroEquipeViewModel)DataContext;
            var equipe = e.Row.Item as EquipeExternaEquipeModel;
            await vm.AddEquipeAsync(equipe);
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

public partial class CadastroEquipeViewModel : ObservableObject
{
    private readonly Context? _dbContext;
    private readonly DataBaseSettings? _dataBaseSettings;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaEquipeModel> equipes;

    public async Task<ObservableCollection<EquipeExternaEquipeModel>> GetEquipesAsync()
    {
        using var _db = new Context();
        var result = await _db.Equipes
            .OrderBy(f => f.id)
            .ToListAsync();
        return new ObservableCollection<EquipeExternaEquipeModel>(result);
    }

    public async Task<bool> AddEquipeAsync(EquipeExternaEquipeModel model)
    {
        using var db = new Context();
        var modelExistente = await db.Equipes.FindAsync(model.id);
        if (modelExistente == null)
            await db.Equipes.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
        return true;
    }
}
