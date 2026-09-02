using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Operacional.Views.Documentos;

/// <summary>
/// Interação lógica para ControleDocumento.xam
/// </summary>
public partial class ControleDocumento : UserControl
{
    DataBaseSettings Setting = DataBaseSettings.Instance;

    public ControleDocumento()
    {
        InitializeComponent();
        DataContext = new ControleDocumentoViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            ControleDocumentoViewModel vm = (ControleDocumentoViewModel)DataContext;
            await vm.GetSiglasAsync();
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

    private async void Aprovado_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        
        try
        {
            if (e.AddedItems.Count == 0)
                return;

            var sigla = e.AddedItems[0] as string;
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            ControleDocumentoViewModel vm = (ControleDocumentoViewModel)DataContext;
            await vm.GetDocumentosAsync(sigla); 
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

    private async void RadGridView_RowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            ControleDocumentoViewModel vm = (ControleDocumentoViewModel)DataContext;

            if (e.Row.Item is ControleDocumentoClienteDTO linha)
                await vm.GravarAsync(
                    new OperacionalControleDocumentoClienteModel 
                    { 
                        id = linha.id, 
                        id_documento = linha.id_documento,
                        sigla = linha.sigla,
                        fecha = linha.fecha,
                        direcionado_resp = linha.direcionado_resp,
                        direcionado_resp_por = linha.direcionado_resp_por,
                        direcionado_resp_em = linha.direcionado_resp_em,
                        em_analise = linha.em_analise,
                        em_analise_por = linha.em_analise_por,
                        em_analise_em = linha.em_analise_em,
                        concluido = linha.concluido,
                        concluido_por = linha.concluido_por,
                        concluido_em = linha.concluido_em,
                        enviado = linha.enviado,
                        enviado_por = linha.enviado_por,
                        enviado_em = linha.enviado_em,
                    });
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

public partial class ControleDocumentoViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<string> siglas;
    [ObservableProperty]
    private ObservableCollection<ControleDocumentoClienteDTO> controleDocumentoClientes;

    public async Task GetSiglasAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<string>(
            @"SELECT sigla
              FROM producao.t_aprovados
              WHERE sigla IS NOT NULL
              GROUP BY sigla
              ORDER BY sigla;");

        Siglas =  new ObservableCollection<string>(result);
    }

    public async Task GetDocumentosAsync(string sigla)
    {
        try
        {
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO operacional.tblcontrole_documento_cliente
                    (id_documento, sigla, direcionado_resp, em_analise, concluido, enviado)
                    SELECT doc.id, @sigla, false, false, false, false
                    FROM operacional.tblcontrole_documento doc
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM operacional.tblcontrole_documento_cliente cliente
                        WHERE cliente.id_documento = doc.id
                          AND cliente.sigla = @sigla
                    );",
                    new { sigla },
                    transaction);

                var resultado = await connection.QueryAsync<ControleDocumentoClienteDTO>(@"
                    SELECT
                        cliente.id,
                        doc.item,
                        doc.quando_enviar,
                        doc.responsavel_liberacao,
                        doc.email_responsavel_liberacao,
                        doc.id AS id_documento,
                        cliente.sigla,
                        cliente.fecha,
                        cliente.direcionado_resp,
                        cliente.direcionado_resp_por,
                        cliente.direcionado_resp_em,
                        cliente.em_analise,
                        cliente.em_analise_por,
                        cliente.em_analise_em,
                        cliente.concluido,
                        cliente.concluido_por,
                        cliente.concluido_em,
                        cliente.enviado,
                        cliente.enviado_por,
                        cliente.enviado_em
                    FROM operacional.tblcontrole_documento doc
                    JOIN operacional.tblcontrole_documento_cliente cliente
                      ON doc.id = cliente.id_documento
                    WHERE cliente.sigla = @sigla
                    ORDER BY doc.quando_enviar, doc.item;",
                    new { sigla },
                    transaction);

                await transaction.CommitAsync();
                ControleDocumentoClientes = new ObservableCollection<ControleDocumentoClienteDTO>(resultado);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task GravarAsync(OperacionalControleDocumentoClienteModel model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO operacional.tblcontrole_documento_cliente
                (id_documento, sigla, fecha, direcionado_resp, direcionado_resp_por,
                 direcionado_resp_em, em_analise, em_analise_por, em_analise_em,
                 concluido, concluido_por, concluido_em, enviado, enviado_por, enviado_em)
                VALUES
                (@id_documento, @sigla, @fecha, @direcionado_resp, @direcionado_resp_por,
                 @direcionado_resp_em, @em_analise, @em_analise_por, @em_analise_em,
                 @concluido, @concluido_por, @concluido_em, @enviado, @enviado_por, @enviado_em)
                RETURNING id;",
                model);

            return;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE operacional.tblcontrole_documento_cliente
            SET
                id_documento = @id_documento,
                sigla = @sigla,
                fecha = @fecha,
                direcionado_resp = @direcionado_resp,
                direcionado_resp_por = @direcionado_resp_por,
                direcionado_resp_em = @direcionado_resp_em,
                em_analise = @em_analise,
                em_analise_por = @em_analise_por,
                em_analise_em = @em_analise_em,
                concluido = @concluido,
                concluido_por = @concluido_por,
                concluido_em = @concluido_em,
                enviado = @enviado,
                enviado_por = @enviado_por,
                enviado_em = @enviado_em
            WHERE id = @id;",
            model);

        if (linhas == 0)
        {
            model.id = 0;
            await GravarAsync(model);
        }

    }
}
