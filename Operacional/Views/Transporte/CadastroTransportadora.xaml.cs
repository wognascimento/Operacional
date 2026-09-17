using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }
}

public partial class CadastroTransportadoraViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<TranportadoraModel> transportadoras;

    public async Task<ObservableCollection<TranportadoraModel>> GetTransportadorasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<TranportadoraModel>(
            @"SELECT *
              FROM operacional.tbltranportadoras
              ORDER BY nometransportadora;");

        return new ObservableCollection<TranportadoraModel>(result);
    }

    public async Task<bool> AddTransportadoraAsync(TranportadoraModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.codtransportadora is null or <= 0)
        {
            model.codtransportadora = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO operacional.tbltranportadoras
                (nometransportadora, cep, endereco, bairro, cidade, uf, ie, ccm,
                 cnpj, ddd, fone_1, fone_2, contato, id_nextel)
                VALUES
                (@nometransportadora, @cep, @endereco, @bairro, @cidade, @uf, @ie, @ccm,
                 @cnpj, @ddd, @fone_1, @fone_2, @contato, @id_nextel)
                RETURNING codtransportadora;",
                model);

            return true;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE operacional.tbltranportadoras
            SET
                nometransportadora = @nometransportadora,
                cep = @cep,
                endereco = @endereco,
                bairro = @bairro,
                cidade = @cidade,
                uf = @uf,
                ie = @ie,
                ccm = @ccm,
                cnpj = @cnpj,
                ddd = @ddd,
                fone_1 = @fone_1,
                fone_2 = @fone_2,
                contato = @contato,
                id_nextel = @id_nextel
            WHERE codtransportadora = @codtransportadora;",
            model);

        if (linhas != 1)
            {
                throw new InvalidOperationException("O registro nao existe mais ou foi alterado por outro usuario. Recarregue a tela; nenhum novo registro foi criado.");
            }

        return true;
    }
}
