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
        if (e.EditAction != GridViewEditAction.Commit)
            return; // Não salva se cancelou a edição

        CargaMontagemViewModel vm = (CargaMontagemViewModel)DataContext;
        var linha = e.EditedItem as QryfrmtranspDetalheModel;
        if (linha != null)
        {
            try
            {
                radGridView.IsEnabled = false;
                var carga = new tbl_cargas_montagem
                    {
                        id = linha.id,
                        siglaserv = linha.siglaserv,
                        num_caminhao = linha.num_caminhao,
                        data = linha.data,
                        data_previsao_chegada = linha.data_previsao_chegada,
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
                    };
                await vm.GravarAsync(carga, linha.data_inicio_montagem);
                linha.id = carga.id;
                foreach (var outra in vm.CargasMontagem.Where(x => x.siglaserv == linha.siglaserv))
                    outra.data_inicio_montagem = linha.data_inicio_montagem;
                radGridView.Rebind();
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
            finally
            {
                radGridView.IsEnabled = true;
            }
        }
    }

}

public partial class CargaMontagemViewModel : ObservableObject
{
    private readonly Dictionary<string, DateTime?> _iniciosMontagem = new();
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
        _iniciosMontagem.Clear();
        foreach (var linha in CargasMontagem)
            if (linha.siglaserv is not null)
                _iniciosMontagem[linha.siglaserv] = linha.data_inicio_montagem;
    }

    public async Task GravarAsync(tbl_cargas_montagem model, DateTime? inicioMontagem)
    {
        if (string.IsNullOrWhiteSpace(model.siglaserv))
            throw new InvalidOperationException("A carga nao possui sigla de servico.");
        using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var id = model.id;

        if (model.id <= 0)
        {
            id = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO operacional.tbl_cargas_montagem
                (siglaserv, data, num_caminhao, m3_contratado, obscarga,
                 trasnportadora, veiculo_programado, data_chegada, obs_saida,
                 local_carga, valor_frete_contratado_caminhao, noite_montagem,
                 obs_externas, obs_frete_contratado, data_previsao_chegada)
                VALUES
                (@siglaserv, @data, @num_caminhao, @m3_contratado, @obscarga,
                 @trasnportadora, @veiculo_programado, @data_chegada, @obs_saida,
                 @local_carga, @valor_frete_contratado_caminhao, @noite_montagem,
                 @obs_externas, @obs_frete_contratado, @data_previsao_chegada)
                RETURNING id;",
                model, transaction);
        }
        else
        {
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
                data_previsao_chegada = @data_previsao_chegada,
                obs_saida = @obs_saida,
                local_carga = @local_carga,
                valor_frete_contratado_caminhao = @valor_frete_contratado_caminhao,
                noite_montagem = @noite_montagem,
                obs_externas = @obs_externas,
                obs_frete_contratado = @obs_frete_contratado
            WHERE id = @id;",
            model, transaction);

        if (linhas == 0)
        {
            throw new InvalidOperationException("A carga nao foi localizada para atualizacao. Reabra a tela e tente novamente.");
        }
        }

        if (!_iniciosMontagem.TryGetValue(model.siglaserv, out var inicioAnterior) || inicioAnterior != inicioMontagem)
        {
            var alterados = await connection.ExecuteAsync(
                "UPDATE operacional.t_data_efetiva SET data_inicio_montagem = @inicioMontagem WHERE siglaserv = @siglaserv;",
                new { model.siglaserv, inicioMontagem }, transaction);
            if (alterados == 0)
                throw new InvalidOperationException("Cadastre a data efetiva deste servico antes de alterar o inicio da montagem.");
        }
        await transaction.CommitAsync();
        model.id = id;
        _iniciosMontagem[model.siglaserv] = inicioMontagem;
    }
}
