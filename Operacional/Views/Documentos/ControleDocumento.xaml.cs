using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }
}

public partial class ControleDocumentoViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> siglas;
    [ObservableProperty]
    private ObservableCollection<ControleDocumentoClienteDTO> controleDocumentoClientes;

    public async Task GetSiglasAsync()
    {
        using var _db = new Context();
        var result = await _db.ProducaoAprovados
            .GroupBy(f => f.sigla)
            .OrderBy(f => f.Key)
            .Select(f => f.Key)
            .ToListAsync();
        Siglas =  new ObservableCollection<string>(result);
    }

    public async Task GetDocumentosAsync(string sigla)
    {
        try
        {
            using var _db = new Context();
            var documentos = await _db.ControleDocumentos
                .OrderBy(f => f.quando_enviar)
                .ThenBy(f => f.item)
                .ToListAsync();

            var documentosCliente = await _db.ControleDocumentoClientes
                .Where(f => f.sigla == sigla)
                .ToListAsync();

            var documentosFaltantes = documentos
                .Where(f => !documentosCliente.Any(s => f.id == s.id_documento))
                .ToList();

            var executionStrategy = _db.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in documentosFaltantes)
                    {
                        await _db.ControleDocumentoClientes.AddAsync(new OperacionalControleDocumentoClienteModel
                        {
                            id_documento = item.id,
                            sigla = sigla,
                            direcionado_resp = false,
                            em_analise = false,
                            concluido = false,
                            enviado = false,
                            //direcionado_resp_em = null,
                            //em_analise_em
                            //concluido_em
                            //enviado_em

                        });
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var resultado = from doc in _db.ControleDocumentos
                                    join cliente in _db.ControleDocumentoClientes
                                    on doc.id equals cliente.id_documento
                                    where cliente.sigla == sigla
                                    orderby doc.quando_enviar, doc.item
                                    select new ControleDocumentoClienteDTO
                                    {
                                        id = cliente.id,
                                        item = doc.item,
                                        quando_enviar = doc.quando_enviar,
                                        responsavel_liberacao = doc.responsavel_liberacao,
                                        email_responsavel_liberacao = doc.email_responsavel_liberacao,
                                        id_documento = doc.id,
                                        sigla = cliente.sigla,
                                        fecha = cliente.fecha,
                                        direcionado_resp = cliente.direcionado_resp,
                                        direcionado_resp_por = cliente.direcionado_resp_por,
                                        direcionado_resp_em = cliente.direcionado_resp_em,
                                        em_analise = cliente.em_analise,
                                        em_analise_por = cliente.em_analise_por,
                                        em_analise_em = cliente.em_analise_em,
                                        concluido  = cliente.concluido,
                                        concluido_por = cliente.concluido_por,
                                        concluido_em = cliente.concluido_em,
                                        enviado = cliente.enviado,
                                        enviado_por = cliente.enviado_por,
                                        enviado_em = cliente.enviado_em
                                    };

                    ControleDocumentoClientes = new ObservableCollection<ControleDocumentoClienteDTO>(await resultado.ToListAsync()); 
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                catch (PostgresException)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task GravarAsync(OperacionalControleDocumentoClienteModel model)
    {
        using var db = new Context();
        var modelExistente = await db.ControleDocumentoClientes.FindAsync(model.id);
        if (modelExistente == null)
            await db.ControleDocumentoClientes.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();

    }
}
