using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls.GridView;

namespace Operacional.Views.Transporte;

/// <summary>
/// Interação lógica para CargaDesmontagem.xam
/// </summary>
public partial class CargaDesmontagem : UserControl
{
    public CargaDesmontagem()
    {
        InitializeComponent();
        DataContext = new CargaDesmontagemViewModel();
        Loaded += CargaDesmontagem_Loaded;
    }

    private async void CargaDesmontagem_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            CargaDesmontagemViewModel vm = (CargaDesmontagemViewModel)DataContext;
            await vm.GetDesmontDetalhesAsync();
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

    private void RadGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.IsInEditMode != true) return;
        if (e.Row.Item is not TranspDesmontDetalheModel linha) return;
        ValidatedGridSave.Save(sender, e, async () =>
        {
            var carga = new t_cargas_desmontagem
            {
                id = linha.id,
                siglaserv = linha.sigla_serv,
                data_chegada_shopping = linha.data_chegada_shopping,
                data_saida_shopping = linha.data_saida_shopping,
                volume = linha.volume,
                caminhao = linha.caminhao,
                prev_volume = linha.volume_carga_desmontagem,
                data_chegada_cipolatti = linha.data_chegada_cipolatti,
                obs = linha.obs,
                transportadora = linha.transportadora,
                descarga_caminhao = linha.descarga_caminhao,
                obs_recebimento = linha.obs_recebimento,
                vl_est_frete = linha.vl_est_frete,
                vl_est_seguro = linha.vl_est_seguro,
                vl_est_icms = linha.vl_est_icms,
                vl_est_total = linha.vl_est_total,
                obs_embalagem = linha.obs_embalagem,
                data_chegada_galpao = linha.data_chegada_galpao,
                hora_chegada_galpao = linha.hora_chegada_galpao,
                placa_caminhao = linha.placa_caminhao,
                obs_frete_caminhao_desmont = linha.obs_frete_caminhao_desmont
            };
            await ((CargaDesmontagemViewModel)DataContext).GravarAsync(carga);
            linha.id = carga.id;
        });
    }
}

public partial class CargaDesmontagemViewModel : ObservableObject
{
    private DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<TranspDesmontDetalheModel> cargasDesmontagem;

    public async Task GetDesmontDetalhesAsync()
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        var sql = @"SELECT * FROM operacional.qrytranspdesmont_detalhes ORDER BY data_chegada_shopping, sigla_serv";
        var lista = (await conn.QueryAsync<TranspDesmontDetalheModel>(sql)).ToList();
        CargasDesmontagem = new ObservableCollection<TranspDesmontDetalheModel>(lista);
    }

    public Task GravarAsync(t_cargas_desmontagem model)
    {
        return CargaDesmontagemRepository.SalvarAsync(model, false);
    }

}
