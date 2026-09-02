using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
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
            Operacional.ErrorDialog.Show(ex, "Erro");
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
            Operacional.ErrorDialog.Show(ex, "Erro");
        }
    }
}

public partial class ContatoViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<EquipeExternaContatoModel> contatos;

    public async Task LoadContatos()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var contatosList = await connection.QueryAsync<EquipeExternaContatoModel>(
            @"SELECT *
              FROM equipe_externa.tbl_contatos
              ORDER BY nome;");

        Contatos = new ObservableCollection<EquipeExternaContatoModel>(contatosList);
    }

    public async Task AdcionarContato(EquipeExternaContatoModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.cod_linha is null or <= 0)
        {
            model.cod_linha = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO equipe_externa.tbl_contatos
                (nome, funcao, tel_1, tel_2, e_mail)
                VALUES (@nome, @funcao, @tel_1, @tel_2, @e_mail)
                RETURNING cod_linha;",
                model);

            return;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_contatos
            SET nome = @nome,
                funcao = @funcao,
                tel_1 = @tel_1,
                tel_2 = @tel_2,
                e_mail = @e_mail
            WHERE cod_linha = @cod_linha;",
            model);

        if (linhas == 0)
        {
            model.cod_linha = null;
            await AdcionarContato(model);
        }
    }
}
