using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Operacional.DataBase.Models;
using Operacional.Views.Despesa;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Operacional.Views.EquipeExterna;

/// <summary>
/// Interação lógica para Contato.xam
/// </summary>
public partial class Contato : UserControl
{
    public Contato()
    {
        InitializeComponent();
        DataContext = new ContatoViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ContatoViewModel vm = (ContatoViewModel)DataContext;
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            await vm.LoadContatos();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ContatoRowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            ContatoViewModel vm = (ContatoViewModel)DataContext;
            if (e.Row is not null && e.Row.DataContext is EquipeExternaContatoModel model)
            {
                if (string.IsNullOrWhiteSpace(model.nome) || string.IsNullOrWhiteSpace(model.funcao) || string.IsNullOrWhiteSpace(model.tel_1))
                {
                    MessageBox.Show("Preencha os campos obrigatórios: Nome, Função e Telefone 1.", "Campos Obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                await vm.AdcionarContato(model);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }
        catch (DbUpdateException ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            if (ex.InnerException is not null)
                MessageBox.Show(ex.InnerException.Message, "Erro de atualização", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(ex.Message, "Erro de atualização", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)  // Para qualquer outro erro
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public partial class ContatoViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<EquipeExternaContatoModel> contatos;

    public async Task LoadContatos()
    {
        using Context context = new();
        var contatosList = await Task.Run(() => context.EquipeExternaContatos.ToList());
        Contatos = new ObservableCollection<EquipeExternaContatoModel>(contatosList);
    }

    public async Task AdcionarContato(EquipeExternaContatoModel model)
    {
        using Context context = new();
        var modelExistente = await context.EquipeExternaContatos.FindAsync(model.cod_linha);
        if (modelExistente == null)
            context.EquipeExternaContatos.Add(model);
        else
            context.Entry(modelExistente).CurrentValues.SetValues(model);

        await context.SaveChangesAsync();
    }
}