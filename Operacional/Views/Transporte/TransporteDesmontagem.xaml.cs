using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Operacional.DataBase;
using Operacional.DataBase.Models;
using Operacional.DataBase.Models.DTOs;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Operacional.Views.Transporte;

/// <summary>
/// Interação lógica para TransporteDesmontagem.xam
/// </summary>
public partial class TransporteDesmontagem : UserControl
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public TransporteDesmontagem()
    {
        InitializeComponent();
        this.DataContext = new TransporteDesmontagemViewModel();
        Loaded += TransporteDesmontagem_Loaded;
    }

    private async void TransporteDesmontagem_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is TransporteDesmontagemViewModel vm)
            {
                await vm.GetTransportesAsync();
                await vm.GetTransportadorasAsync();
            }
        }
        catch (PostgresException ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar dados", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro inesperado: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RadGridView_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        try
        {

            TransporteDesmontagemViewModel vm = (TransporteDesmontagemViewModel)DataContext;
            if (!e.Row.IsInEditMode)
                return;

            if (e.Row.Item is TranspDesmontDTO item)
            {
                //MessageBox.Show($"Linha alterada: {item.SiglaServ}, {item.numero_de_caminhoes}");
                var novoTransporte = new OperacionalTranporteDesmontagemModel
                {
                    siglaserv = item.siglaserv,
                    prazo_chegada = Convert.ToInt16(item.prazo_chegada),
                    volume_carga_desmontagem = Convert.ToInt16(item.volume_carga_desmontagem),
                    num_caminhoes_desmont = Convert.ToInt16(item.num_caminhoes_desmont),
                    transportadora = item.transportadora,
                    alterado_por = BaseSettings.Username,
                    data_altera = DateTime.Now
                };
                bool sucesso = await vm.AtualizarTransporteAsync(novoTransporte);
                if (sucesso == false)
                {
                    e.IsValid = false; // Impede que a linha seja confirmada
                    MessageBox.Show("Erro ao salvar no banco! Verifique os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    //if (item.num_caminhoes_desmont != e.OldValues["num_caminhoes_desmont"] as int?)
                    //{

                    DateTime data = item.data_inicio_desmontagem ?? new DateTime(DateTime.Now.Year, 12, 25);
                    await vm.SincronizarCaminhoes(item.siglaserv, item.num_caminhoes_desmont, data, item.transportadora);
                    item.Cargas = await vm.CaminhoesSigla(item.siglaserv);
                    radGridView.Rebind(); // Recarrega todos os dados da grade

                    //}

                }
            }

        }
        catch (DbUpdateException ex)
        {
            e.IsValid = false;
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            //MessageBox.Show(ex.InnerException.Message);
        }
    }

    private void RadGridViewFilho_RowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {

    }
}

public partial class TransporteDesmontagemViewModel : ObservableObject
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    [ObservableProperty]
    private ObservableCollection<TranportadoraModel> transportadoras;

    [ObservableProperty]
    private ObservableCollection<TranspDesmontDTO> transportes;

    public async Task GetTransportadorasAsync()
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        var sql = @"SELECT * FROM operacional.tbltranportadoras ORDER BY nometransportadora";
        var lista = (await conn.QueryAsync<TranportadoraModel>(sql)).ToList();
        Transportadoras = new ObservableCollection<TranportadoraModel>(lista);
    }

    // método convertido para Dapper
    public async Task GetTransportesAsync()
    {

        var sqlTransporte = @"
            SELECT
                *
            FROM operacional.qryfrmtranspdesmont;"; // ajuste schema/nome se necessário

        var sqlCargas = @"
            SELECT
                *
            FROM operacional.t_cargas_desmontagem;"; // ajuste schema/nome se necessário

        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        await conn.OpenAsync();

        // busca ambas as listas em paralelo
        var transTask = await conn.QueryAsync<TranspDesmontDTO>(sqlTransporte);
        var cargasTask = await conn.QueryAsync<OperacionalCargaDesmontagemModel>(sqlCargas);

        var qryList = (transTask).ToList();
        var cargasList = (cargasTask).ToList();

        // Função local para mapear cargas (mantém a lógica de ordenação e projeção)
        ObservableCollection<OperacionalCargaDesmontagemModel> MapearCargas(string siglaServ)
        {
            var cargasParaSigla = cargasList
                .Where(c => string.Equals(c.siglaserv, siglaServ, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.caminhao)
                .Select(c => new OperacionalCargaDesmontagemModel
                {
                    id = c.id,
                    siglaserv = c.siglaserv,
                    data_chegada_shopping = c.data_chegada_shopping,
                    data_saida_shopping = c.data_saida_shopping,
                    volume = c.volume,
                    caminhao = c.caminhao,
                    prev_volume = c.prev_volume,
                    data_chegada_cipolatti = c.data_chegada_cipolatti,
                    obs = c.obs,
                    transportadora = c.transportadora,
                    confirmado = c.confirmado,
                    descarga_caminhao = c.descarga_caminhao,
                    obs_recebimento = c.obs_recebimento,
                    vl_est_frete = c.vl_est_frete,
                    vl_est_seguro = c.vl_est_seguro,
                    vl_est_icms = c.vl_est_icms,
                    vl_est_total = c.vl_est_total,
                    obs_embalagem = c.obs_embalagem,
                    data_chegada_galpao = c.data_chegada_galpao,
                    hora_chegada_galpao = c.hora_chegada_galpao,
                    placa_caminhao = c.placa_caminhao,
                    obs_frete_caminhao_desmont = c.obs_frete_caminhao_desmont
                })
                .ToList();

            return new ObservableCollection<OperacionalCargaDesmontagemModel>(cargasParaSigla);
        }

        // Projeta a lista principal associando as cargas
        var resultado = new ObservableCollection<TranspDesmontDTO>(
            [.. qryList.Select(q => new TranspDesmontDTO
                {
                    siglaserv = q.siglaserv,
                    transporte = q.transporte,
                    regiao = q.regiao,
                    cidade = q.cidade,
                    prazo_chegada = q.prazo_chegada,
                    volume_carga_desmontagem = q.volume_carga_desmontagem,
                    data_inicio_desmontagem = q.data_inicio_desmontagem,
                    data_final_desmontagem = q.data_final_desmontagem,
                    data_libera_area_desmontagem = q.data_libera_area_desmontagem,
                    num_caminhoes_desmont = q.num_caminhoes_desmont,
                    transportadora = q.transportadora,
                    ok = q.ok,
                    local_descarga = q.local_descarga,
                    comboio = q.comboio,
                    valor_frete_desmont = q.valor_frete_desmont,
                    cubagem_por_produto = q.cubagem_por_produto,
                    cubagem_expedida = q.cubagem_expedida,
                    perc_shop = q.perc_shop,
                    data_altera = q.data_altera,
                    alterado_por = q.alterado_por,
                    sigla_serv = q.sigla_serv,
                    Cargas = MapearCargas(q.siglaserv)
                })]
        );

        Transportes = resultado;

    }

    public async Task<bool> AtualizarTransporteAsync(OperacionalTranporteDesmontagemModel transporteAtualizado)
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);

        // 1. Verifica se existe
        var sqlExiste = @"SELECT 1 FROM operacional.t_tranportes_desmont WHERE siglaserv = @siglaserv";
        var existe = await conn.ExecuteScalarAsync<int?>(sqlExiste, new { transporteAtualizado.siglaserv });

        if (existe == null)
            return false; // Não encontrado

        // 2. Atualiza somente os campos enviados
        var sqlUpdate = @"
        UPDATE operacional.t_tranportes_desmont
            SET 
                prazo_chegada = @prazo_chegada,
                volume_carga_desmontagem = @volume_carga_desmontagem,
                num_caminhoes_desmont = @num_caminhoes_desmont,
                transportadora = @transportadora
            WHERE siglaserv = @siglaserv
        ";

        var linhasAfetadas = await conn.ExecuteAsync(sqlUpdate, transporteAtualizado);

        return linhasAfetadas > 0;
    }
    /*
    public async Task SincronizarCaminhoes(string siglaServ, int? totalCaminhoes, DateTime? data, string transportadora)
    {
        try
        {
            using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
            await conn.OpenAsync();

            using var trans = conn.BeginTransaction();

            // 1. Buscar caminhões existentes
            var sqlSelect = @"SELECT * FROM operacional.t_cargas_desmontagem WHERE siglaserv = @siglaserv ORDER BY caminhao";
            var caminhoesExistentes = (await conn.QueryAsync<OperacionalCargaDesmontagemModel>(sqlSelect, new { siglaServ }, transaction: trans)).ToList();

            int atuais = caminhoesExistentes.Count;

            // 2. Adicionar caminhões faltantes
            if (atuais < totalCaminhoes)
            {
                for (int i = atuais + 1; i <= totalCaminhoes; i++)
                {
                    var novo = new OperacionalCargaDesmontagemModel
                    {
                        siglaserv = siglaServ,
                        caminhao = i.ToString().PadLeft(2, '0'),
                        data_chegada_shopping = data.Value.AddDays(i - 1),
                        transportadora = transportadora,
                        placa_caminhao = null
                    };

                    var sqlInsert = @"
                    INSERT INTO operacional.t_cargas_desmontagem
                        (siglaserv, caminhao, data_chegada_shopping, transportadora, placa_caminhao)
                        VALUES (@siglaserv, @caminhao, @data_chegada_shopping, @transportadora, @placa_caminhao)
                    ";

                    await conn.ExecuteAsync(sqlInsert, novo, transaction: trans);
                }
            }
            // 3. Remover excedentes
            else if (atuais > totalCaminhoes)
            {
                var excedentes = caminhoesExistentes.Skip((int)totalCaminhoes).ToList();

                var sqlDelete = @"DELETE FROM operacional.t_cargas_desmontagem WHERE id = @id";

                foreach (var cam in excedentes)
                    await conn.ExecuteAsync(sqlDelete, new { cam.id }, transaction: trans);
            }

            // 4. Commit
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Erro inesperado.", ex);
        }
    }
    */
    public async Task SincronizarCaminhoes(
        string siglaServ,
        int? totalCaminhoes,
        DateTime? data,
        string transportadora)
    {
        try
        {
            using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
            await conn.OpenAsync();

            using var trans = conn.BeginTransaction();

            // ============================================
            // 1. Buscar caminhões existentes
            // ============================================
            var sqlSelect = @"
                SELECT * 
                FROM operacional.t_cargas_desmontagem 
                WHERE siglaserv = @siglaServ 
                ORDER BY caminhao;
            ";

            var caminhoesExistentes = (await conn.QueryAsync<OperacionalCargaDesmontagemModel>(
                sqlSelect, new { siglaServ }, trans)).ToList();

            int atuais = caminhoesExistentes.Count;

            // ============================================
            // 2. Atualizar os caminhões existentes
            // ============================================
            foreach (var cam in caminhoesExistentes)
            {
                bool precisaUpdate =
                    cam.transportadora != transportadora ||
                    cam.data_chegada_shopping != data;

                if (precisaUpdate)
                {
                    var sqlUpdate = @"
                        UPDATE operacional.t_cargas_desmontagem
                        SET 
                            transportadora = @transportadora,
                            data_chegada_shopping = @data
                        WHERE id = @id
                        RETURNING *;
                    ";

                    var updated = await conn.QuerySingleAsync<OperacionalCargaDesmontagemModel>(
                        sqlUpdate,
                        new
                        {
                            id = cam.id,
                            transportadora,
                            data
                        },
                        trans
                    );

                    // Chamar sincronização de UPDATE
                    await SyncCargaParaRomaneio(conn, trans, updated, cam, "UPDATE");
                }
            }

            // ============================================
            // 3. Adicionar caminhões faltantes
            // ============================================
            if (atuais < totalCaminhoes)
            {
                for (int i = atuais + 1; i <= totalCaminhoes; i++)
                {
                    var novo = new OperacionalCargaDesmontagemModel
                    {
                        siglaserv = siglaServ,
                        caminhao = i.ToString().PadLeft(2, '0'),
                        data_chegada_shopping = data.Value.AddDays(i - 1),
                        transportadora = transportadora,
                        placa_caminhao = null
                    };

                    var sqlInsert = @"
                        INSERT INTO operacional.t_cargas_desmontagem
                            (siglaserv, caminhao, data_chegada_shopping, transportadora, placa_caminhao)
                        VALUES 
                            (@siglaserv, @caminhao, @data_chegada_shopping, @transportadora, @placa_caminhao)
                        RETURNING *;
                    ";

                    var inserted = await conn.QuerySingleAsync<OperacionalCargaDesmontagemModel>(
                        sqlInsert, novo, trans);

                    await SyncCargaParaRomaneio(conn, trans, inserted, null, "INSERT");
                }
            }

            // ============================================
            // 4. Remover excedentes
            // ============================================
            else if (atuais > totalCaminhoes)
            {
                var excedentes = caminhoesExistentes
                    .Skip((int)totalCaminhoes)
                    .ToList();

                var sqlDelete = @"DELETE FROM operacional.t_cargas_desmontagem WHERE id = @id";

                foreach (var cam in excedentes)
                {
                    await SyncCargaParaRomaneio(conn, trans, null, cam, "DELETE");

                    await conn.ExecuteAsync(sqlDelete, new { cam.id }, trans);
                }
            }

            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Erro inesperado.", ex);
        }
    }

    private async Task SyncCargaParaRomaneio(
        IDbConnection conn,
        IDbTransaction trans,
        OperacionalCargaDesmontagemModel newRow,
        OperacionalCargaDesmontagemModel oldRow,
        string operacao)
    {
        // ============================================
        // DELETE → remover do romaneio
        // ============================================
        if (operacao == "DELETE")
        {
            int numero = int.Parse(oldRow.caminhao);

            string sql = @"
                DELETE FROM expedicao.t_romaneio
                WHERE shopping_destino = @sigla
                  AND numero_caminhao = @num
                  AND operacao = 'DESCARREGAMENTO SHOPPING';
            ";

            await conn.ExecuteAsync(sql, new
            {
                sigla = oldRow.siglaserv,
                num = numero
            }, trans);

            return;
        }

        // ============================================
        // INSERT / UPDATE
        // ============================================

        // 1. Buscar codtransportadora (corrigido)
        int? codTransportadora = null;

        if (!string.IsNullOrWhiteSpace(newRow.transportadora))
        {
            codTransportadora = await conn.ExecuteScalarAsync<int?>(@"
                SELECT codtransportadora
                FROM operacional.tbltranportadoras
                WHERE unaccent(lower(nometransportadora)) = unaccent(lower(@nome))
                LIMIT 1;
            ", new { nome = newRow.transportadora.Trim() }, trans);
        }

        int numeroCaminhao = int.Parse(newRow.caminhao);

        // 2. verificar se existe no romaneio
        bool existe = await conn.ExecuteScalarAsync<bool>(@"
            SELECT 1
            FROM expedicao.t_romaneio
            WHERE shopping_destino = @sigla
              AND numero_caminhao = @num
              AND operacao = 'DESCARREGAMENTO SHOPPING'
            LIMIT 1;
        ", new { sigla = newRow.siglaserv, num = numeroCaminhao }, trans);

        // 3. UPDATE se existe
        if (existe)
        {
            await conn.ExecuteAsync(@"
                UPDATE expedicao.t_romaneio
                SET 
                    data_carregamento = @data,
                    codtransportadora = COALESCE(@codt, codtransportadora)
                WHERE shopping_destino = @sigla
                  AND numero_caminhao = @num
                  AND operacao = 'DESCARREGAMENTO SHOPPING';
            ",
                new
                {
                    data = newRow.data_chegada_shopping,
                    codt = codTransportadora, // se null, mantém a antiga
                    sigla = newRow.siglaserv,
                    num = numeroCaminhao
                }, trans);
        }
        else
        {
            // 4. INSERT se não existe
            await conn.ExecuteAsync(@"
                INSERT INTO expedicao.t_romaneio
                    (data_carregamento, codtransportadora, numero_caminhao, shopping_destino, operacao)
                VALUES
                    (@data, @codt, @num, @sigla, 'DESCARREGAMENTO SHOPPING');
            ",
                new
                {
                    data = newRow.data_chegada_shopping,
                    codt = codTransportadora,
                    num = numeroCaminhao,
                    sigla = newRow.siglaserv
                }, trans);
        }
    }




    public async Task<ObservableCollection<OperacionalCargaDesmontagemModel>> CaminhoesSigla(string siglaServ)
    {
        using var conn = new NpgsqlConnection(BaseSettings.ConnectionString);
        var sql = @"SELECT * FROM operacional.t_cargas_desmontagem WHERE siglaserv = @siglaServ ORDER BY caminhao";
        var lista = (await conn.QueryAsync<OperacionalCargaDesmontagemModel>(sql, new { siglaServ })).ToList();
        return new ObservableCollection<OperacionalCargaDesmontagemModel>(lista);
    }


}
