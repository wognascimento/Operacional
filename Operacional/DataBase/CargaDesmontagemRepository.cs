using Dapper;
using Npgsql;
using Operacional.DataBase.Models;

namespace Operacional.DataBase;

internal static class CargaDesmontagemRepository
{
    public static async Task SalvarAsync(t_cargas_desmontagem model, bool transporte)
    {
        if (string.IsNullOrWhiteSpace(model.siglaserv) ||
            !int.TryParse(model.caminhao, out var numero) || numero <= 0)
            throw new InvalidOperationException("Informe o servico e um numero de caminhao valido.");

        // Only fields represented by each editor may overwrite stored values.
        string[] campos = transporte
            ? ["data_chegada_shopping", "data_saida_shopping", "data_chegada_cipolatti",
               "volume", "confirmado", "obs", "descarga_caminhao", "obs_recebimento",
               "data_chegada_galpao", "hora_chegada_galpao", "placa_caminhao", "transportadora", "obs_embalagem"]
            : ["data_chegada_shopping", "data_saida_shopping", "data_chegada_cipolatti",
               "volume", "prev_volume", "obs", "obs_recebimento", "data_chegada_galpao",
               "hora_chegada_galpao", "placa_caminhao", "obs_embalagem",
               "vl_est_frete", "vl_est_seguro", "vl_est_icms", "vl_est_total"];
        using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        var existente = await conn.QuerySingleOrDefaultAsync<t_cargas_desmontagem>(
            "SELECT * FROM operacional.t_cargas_desmontagem WHERE id = @id FOR UPDATE;",
            new { model.id }, transaction);
        var id = model.id;
        if (id > 0 && existente == null)
            throw new InvalidOperationException("Carga excluida por outro usuario. Recarregue a tela.");
        if (existente != null && (existente.siglaserv != model.siglaserv || existente.caminhao != model.caminhao))
            throw new InvalidOperationException("O vinculo da carga mudou. Recarregue a tela.");

        var alterados = campos.Where(c => existente == null ||
            !Equals(typeof(t_cargas_desmontagem).GetProperty(c)!.GetValue(model),
                    typeof(t_cargas_desmontagem).GetProperty(c)!.GetValue(existente))).ToArray();
        if (id <= 0)
        {
            var colunas = new[] { "siglaserv", "caminhao" }.Concat(campos).ToArray();
            id = await conn.QuerySingleAsync<long>(
                $"INSERT INTO operacional.t_cargas_desmontagem ({string.Join(", ", colunas)}) " +
                $"VALUES ({string.Join(", ", colunas.Select(c => "@" + c))}) RETURNING id;",
                model, transaction);
        }
        else if (alterados.Length > 0)
        {
            if (await conn.ExecuteAsync(
                $"UPDATE operacional.t_cargas_desmontagem SET {string.Join(", ", alterados.Select(c => c + " = @" + c))} WHERE id = @id;",
                model, transaction) != 1)
                throw new InvalidOperationException("Nao foi possivel atualizar a carga.");
        }

        var sets = new List<string>();
        if (alterados.Contains("placa_caminhao")) sets.Add("placa_carroceria = @placa");
        if (alterados.Contains("data_chegada_cipolatti")) sets.Add("data_carregamento = @data");
        if (sets.Count > 0)
            await conn.ExecuteAsync(
                $"UPDATE expedicao.t_romaneio SET {string.Join(", ", sets)} " +
                "WHERE shopping_destino = @sigla AND numero_caminhao = @numero AND operacao = 'DESCARREGAMENTO SHOPPING';",
                new { placa = model.placa_caminhao, data = model.data_chegada_cipolatti,
                    sigla = model.siglaserv, numero }, transaction);
        await transaction.CommitAsync();
        model.id = id;
    }
}
