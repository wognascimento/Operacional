using Dapper;
using CommunityToolkit.Mvvm.ComponentModel;
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
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
                Operacional.ErrorDialog.Show(ex, "Erro ao salvar");
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
            Operacional.ErrorDialog.Show(ex, "Erro inesperado");
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }
}

public partial class CargaMontagemViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<QryfrmtranspDetalheModel> cargasMontagem;

    public async Task GetCargasMontagemAsync()
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        var result = await connection.QueryAsync<QryfrmtranspDetalheModel>(
            @"SELECT *
              FROM operacional.qryfrmtransp_detalhe
              ORDER BY data, siglaserv;");

        CargasMontagem = new ObservableCollection<QryfrmtranspDetalheModel>(result);
    }

    public async Task GravarAsync(tbl_cargas_montagem model)
    {
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);

        if (model.id <= 0)
        {
            model.id = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO operacional.tbl_cargas_montagem
                (siglaserv, data, num_caminhao, m3_contratado, obscarga,
                 trasnportadora, veiculo_programado, data_chegada, obs_saida,
                 local_carga, valor_frete_contratado_caminhao, noite_montagem,
                 obs_externas, obs_frete_contratado)
                VALUES
                (@siglaserv, @data, @num_caminhao, @m3_contratado, @obscarga,
                 @trasnportadora, @veiculo_programado, @data_chegada, @obs_saida,
                 @local_carga, @valor_frete_contratado_caminhao, @noite_montagem,
                 @obs_externas, @obs_frete_contratado)
                RETURNING id;",
                model);

            return;
        }

        var linhas = await connection.ExecuteAsync(@"
            UPDATE operacional.tbl_cargas_montagem
            SET
                siglaserv = @siglaserv,
                data = @data,
                num_caminhao = @num_caminhao,
                m3_contratado = @m3_contratado,
                obscarga = @obscarga,
                trasnportadora = @trasnportadora,
                veiculo_programado = @veiculo_programado,
                data_chegada = @data_chegada,
                obs_saida = @obs_saida,
                local_carga = @local_carga,
                valor_frete_contratado_caminhao = @valor_frete_contratado_caminhao,
                noite_montagem = @noite_montagem,
                obs_externas = @obs_externas,
                obs_frete_contratado = @obs_frete_contratado
            WHERE id = @id;",
            model);

        if (linhas == 0)
        {
            model.id = 0;
            await GravarAsync(model);
        }
    }
}
