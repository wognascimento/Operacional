using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

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
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void RadGridView_RowEditEnded(object sender, Telerik.Windows.Controls.GridViewRowEditEndedEventArgs e)
    {
        if (e.EditAction == GridViewEditAction.Cancel)
            return; // Não salva se cancelou a edição

        CargaDesmontagemViewModel vm = (CargaDesmontagemViewModel)DataContext;
        var linha = e.NewData as TranspDesmontDetalheModel;
        if (linha != null)
        {
            try
            {
                await vm.GravarAsync(
                    new t_cargas_desmontagem
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
                        confirmado = "N", // Sempre grava como "N" (não confirmado)
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
}

public partial class CargaDesmontagemViewModel : ObservableObject
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<TranspDesmontDetalheModel> cargasDesmontagem;

    public async Task GetDesmontDetalhesAsync()
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        var sql = @"SELECT * FROM operacional.qrytranspdesmont_detalhes ORDER BY data_chegada_shopping, sigla_serv";
        var lista = (await conn.QueryAsync<TranspDesmontDetalheModel>(sql)).ToList();
        CargasDesmontagem = new ObservableCollection<TranspDesmontDetalheModel>(lista);
    }

    public async Task GravarAsync(t_cargas_desmontagem model)
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);

        var sqlSelect = @"SELECT * FROM operacional.t_cargas_desmontagem WHERE id = @id";
        var existente = await conn.QueryFirstOrDefaultAsync<t_cargas_desmontagem?>(sqlSelect, new { model.id });

        if (existente == null)
        {
            // INSERT
            var sqlInsert = @"
                INSERT INTO operacional.t_cargas_desmontagem
                (   
                    siglaserv,
                    data_chegada_shopping,
                    data_saida_shopping,
                    volume,
                    caminhao,
                    prev_volume,
                    data_chegada_cipolatti,
                    obs,
                    transportadora,
                    confirmado,
                    descarga_caminhao,
                    obs_recebimento,
                    vl_est_frete,
                    vl_est_seguro,
                    vl_est_icms,
                    vl_est_total,
                    obs_embalagem,
                    data_chegada_galpao,
                    hora_chegada_galpao,
                    placa_caminhao,
                    obs_frete_caminhao_desmont,
                )
                VALUES
                (
                    @siglaserv,
                    @data_chegada_shopping,
                    @data_saida_shopping,
                    @volume,
                    @caminhao,
                    @prev_volume,
                    @data_chegada_cipolatti,
                    @obs,
                    @transportadora,
                    @confirmado,
                    @descarga_caminhao,
                    @obs_recebimento,
                    @vl_est_frete,
                    @vl_est_seguro,
                    @vl_est_icms,
                    @vl_est_total,
                    @obs_embalagem,
                    @data_chegada_galpao,
                    @hora_chegada_galpao,
                    @placa_caminhao,
                    @obs_frete_caminhao_desmont
                )
                RETURNING id;
            ";

            model.id = await conn.ExecuteScalarAsync<int>(sqlInsert, model);
        }
        else
        {
            // UPDATE
            /*var sqlUpdate = @"
                UPDATE operacional.t_cargas_desmontagem SET
                    siglaserv =	@siglaserv,
                    data_chegada_shopping =	@data_chegada_shopping,
                    data_saida_shopping	= @data_saida_shopping,
                    volume = @volume,
                    caminhao = @caminhao,
                    prev_volume	= @prev_volume,
                    data_chegada_cipolatti = @data_chegada_cipolatti,
                    obs = @obs,
                    transportadora = @transportadora,
                    confirmado = @confirmado,
                    descarga_caminhao =	@descarga_caminhao,
                    obs_recebimento = @obs_recebimento,
                    vl_est_frete = @vl_est_frete,
                    vl_est_seguro =	@vl_est_seguro,
                    vl_est_icms = @vl_est_icms,
                    vl_est_total = @vl_est_total,
                    obs_embalagem =	@obs_embalagem,
                    data_chegada_galpao	= @data_chegada_galpao,
                    hora_chegada_galpao	= @hora_chegada_galpao,
                    placa_caminhao	= @placa_caminhao,
                    obs_frete_caminhao_desmont = @obs_frete_caminhao_desmont
                WHERE id = @id;
            ";*/

            var tipo = typeof(t_cargas_desmontagem);

            // 2) Lista de SETs só dos alterados
            var setList = new List<string>();
            var parametros = new DynamicParameters();

            foreach (var prop in tipo.GetProperties())
            {
                if (prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var valorNovo = prop.GetValue(model);
                var valorAntigo = prop.GetValue(existente);

                // Ignora valores nulos do modelo novo
                // (você pode mudar esse comportamento)
                if (valorNovo == null)
                    continue;

                // Só adiciona se mudou
                if (!Equals(valorNovo, valorAntigo))
                {
                    setList.Add($"{prop.Name} = @{prop.Name}");
                    parametros.Add(prop.Name, valorNovo);
                }
            }

            // Se nada mudou, não atualizar
            if (setList.Count == 0)
                return;

            // 3) Completar parâmetros com @id
            parametros.Add("id", model.id);

            // 4) Montar SQL final
            var sqlUpdate = $@"
                UPDATE operacional.t_cargas_desmontagem
                SET {string.Join(", ", setList)}
                WHERE id = @id;
            ";

            await conn.ExecuteAsync(sqlUpdate, model);

            string[] campos = [
                "placa_caminhao",
                "data_chegada_cipolatti",
                "transportadora"
            ];

            // verifica se algum dos campos monitorados realmente foi alterado
            bool camposAlterados = setList.Any(s => campos.Any(c => s.Contains(c)));

            if (camposAlterados)
            {
                var updateServicosList = new List<string>();
                //var parametros = new DynamicParameters();
                parametros = new DynamicParameters();

                // SE placa mudou → incluir no UPDATE
                if (setList.Any(s => s.Contains("placa_caminhao")))
                {
                    updateServicosList.Add("placa_carroceria = @placa_caminhao");
                    parametros.Add("placa_caminhao", model.placa_caminhao);
                }

                // SE data mudou → incluir no UPDATE
                if (setList.Any(s => s.Contains("data_chegada_cipolatti")))
                {
                    updateServicosList.Add("data_carregamento = @data_chegada_cipolatti");
                    parametros.Add("data_chegada_cipolatti", model.data_chegada_cipolatti);
                }

                // (transportadora não tem equivalente no romaneio, mas se tiver você coloca aqui)

                // se nenhum campo do romaneio mudou → não faz nada
                if (updateServicosList.Count == 0)
                    return;

                // parametros fixos
                parametros.Add("siglaserv", model.siglaserv);
                parametros.Add("caminhao", int.Parse(model.caminhao));

                var sqlUpdateServicos = $@"
                    UPDATE expedicao.t_romaneio
                    SET {string.Join(", ", updateServicosList)}
                    WHERE shopping_destino = @siglaserv
                      AND numero_caminhao = @caminhao
                      AND operacao = 'DESCARREGAMENTO SHOPPING';
                ";

                await conn.ExecuteAsync(sqlUpdateServicos, parametros);
            }

        }
    }

}