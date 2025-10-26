using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Operacional.Views.Transporte;

/// <summary>
/// Interação lógica para CadastroTransportadora.xam
/// </summary>
public partial class CadastroTransportadora : UserControl
{
    public CadastroTransportadora()
    {
        InitializeComponent();
        DataContext = new CadastroTransportadoraViewModel();
        Loaded += CadastroTransportadora_Loaded;
    }

    private async void CadastroTransportadora_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroTransportadoraViewModel vm = (CadastroTransportadoraViewModel)DataContext;
            vm.Transportadoras = await vm.GetTransportadorasAsync();
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

    private async void TransportadoraRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CadastroTransportadoraViewModel vm = (CadastroTransportadoraViewModel)DataContext;
            var transportadora = e.Row.Item as TranportadoraModel;
            await vm.AddTransportadoraAsync(transportadora);
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

public partial class CadastroTransportadoraViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TranportadoraModel> transportadoras;

    public async Task<ObservableCollection<TranportadoraModel>> GetTransportadorasAsync()
    {
        using var _db = new Context();
        var result = await _db.Tranportadoras
            .OrderBy(f => f.nometransportadora)
            .ToListAsync();
        return new ObservableCollection<TranportadoraModel>(result);
    }

    public async Task<bool> AddTransportadoraAsync(TranportadoraModel model)
    {
        using var db = new Context();
        var modelExistente = await db.Tranportadoras.FindAsync(model.codtransportadora);
        if (modelExistente == null)
            await db.Tranportadoras.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
        return true;
    }
}
