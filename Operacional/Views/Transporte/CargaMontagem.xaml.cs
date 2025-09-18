using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using Operacional.Views.Documentos;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Operacional.Views.Transporte;

/// <summary>
/// Interação lógica para CargaMontagem.xam
/// </summary>
public partial class CargaMontagem : UserControl
{
    public CargaMontagem()
    {
        InitializeComponent();
        DataContext = new CargaMontagemViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CargaMontagemViewModel vm = (CargaMontagemViewModel)DataContext;
            await vm.GetCargasMontagemAsync();
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

    private async void RadGridView_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
    {
        if (e.EditAction == GridViewEditAction.Cancel)
            return; // Não salva se cancelou a edição

        CargaMontagemViewModel vm = (CargaMontagemViewModel)DataContext;
        var linha = e.NewData as QryfrmtranspDetalheModel;
        if (linha != null)
        {
            try
            {
                await vm.GravarAsync(
                    new tbl_cargas_montagem
                    {
                        id = linha.id,
                        siglaserv = linha.siglaserv,
                        num_caminhao = linha.num_caminhao,
                        data = linha.data,
                        noite_montagem = linha.noite_montagem,
                        m3_contratado = linha.m3_contratado,
                        data_chegada = linha.data_chegada,
                        obscarga = linha.obscarga,
                        veiculo_programado = linha.veiculo_programado,
                        trasnportadora = linha.trasnportadora,
                        obs_saida = linha.obs_saida,
                        local_carga = linha.local_carga,
                        obs_externas = linha.obs_externas,
                        valor_frete_contratado_caminhao = linha.valor_frete_contratado_caminhao,
                        obs_frete_contratado = linha.obs_frete_contratado,
                    });
                // Opcional: mostrar mensagem de sucesso
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
            {                
                // Tratar erro específico do PostgreSQL
                MessageBox.Show($"Erro do banco: {pgEx.MessageText}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                //e.EditAction = GridViewEditAction.Cancel; // Cancela a edição
            }
            catch (Exception ex)
            {
                // Tratar erro e possivelmente reverter alterações
                MessageBox.Show($"Erro ao salvar: {ex.Message}");
                //e.EditAction = GridViewEditAction.Cancel; // Cancela a edição
            }
        }
    }

    private async void radGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CargaMontagemViewModel vm = (CargaMontagemViewModel)DataContext;

            if (e.Row.Item is QryfrmtranspDetalheModel linha)
                await vm.GravarAsync(
                    new tbl_cargas_montagem
                    {
                        id = linha.id,
                        siglaserv = linha.siglaserv,
                        num_caminhao = linha.num_caminhao,
                        data = linha.data,
                        noite_montagem  = linha.noite_montagem,
                        m3_contratado = linha.m3_contratado,
                        data_chegada = linha.data_chegada,
                        obscarga = linha.obscarga,
                        veiculo_programado = linha.veiculo_programado,
                        trasnportadora = linha.trasnportadora,
                        obs_saida   = linha.obs_saida,
                        local_carga = linha.local_carga,
                        obs_externas = linha.obs_externas,
                        valor_frete_contratado_caminhao = linha.valor_frete_contratado_caminhao,
                        obs_frete_contratado = linha.obs_frete_contratado,
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

public partial class CargaMontagemViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<QryfrmtranspDetalheModel> cargasMontagem;

    public async Task GetCargasMontagemAsync()
    {
        using var _db = new Context();
        var result = await _db.QryCargasMontagem
            .OrderBy(f => f.data)
            .ThenBy(f => f.siglaserv)
            .ToListAsync();
        CargasMontagem = new ObservableCollection<QryfrmtranspDetalheModel>(result);
    }

    public async Task GravarAsync(tbl_cargas_montagem model)
    {
        using var db = new Context();
        var modelExistente = await db.cargasmontagens.FindAsync(model.id);
        if (modelExistente == null)
            await db.cargasmontagens.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();

    }
}