using Dapper;
using Npgsql;
using Operacional.DataBase.Models;

namespace Operacional.DataBase;

internal static class NotaEquipeRepository
{
    public static async Task<bool> SalvarAsync(RelatorioDetalheModel model)
    {
        if (model.envia_fluxo || model.codrelatorio <= 0 || string.IsNullOrWhiteSpace(model.tipo_detalhe))
            throw new InvalidOperationException("Informe o relatorio e o tipo. Notas enviadas ao fluxo nao podem ser alteradas.");
        using var connection = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Serialize balance recalculation with other notes for the same report.
        var parent = await connection.QuerySingleOrDefaultAsync<long?>(@"
            SELECT cod_relatorio FROM equipe_externa.tblrelatorio_pagamento
            WHERE cod_relatorio = @codrelatorio AND id_equipe = @id_equipe FOR UPDATE;",
            model, transaction);
        if (parent == null) throw new InvalidOperationException("Relatorio nao encontrado para esta equipe.");
        var id = model.cod_detalhe_relatorio;
        model.alterado_por = DataBaseSettings.Instance.Username;
        model.alterado_em = DateTime.Now;
        if (id is null or <= 0)
        {
            model.inserido_por = DataBaseSettings.Instance.Username;
            model.inserido_em = DateTime.Now;
            id = await connection.QuerySingleAsync<long>(@"
                INSERT INTO equipe_externa.tbl_detalhes_relatorio
                (codrelatorio, tipo_detalhe, valor_detalhe, data, descricao, data_pagto,
                 numero_nf, empresa_nf, id_equipe, empresa_pagadora, cliente, envia_fluxo,
                 inserido_por, inserido_em)
                VALUES (@codrelatorio, @tipo_detalhe, @valor_detalhe, @data, @descricao, @data_pagto,
                 @numero_nf, @empresa_nf, @id_equipe, @empresa_pagadora, @cliente, false,
                 @inserido_por, @inserido_em)
                RETURNING cod_detalhe_relatorio;", model, transaction);
        }
        else if (await connection.ExecuteAsync(@"
            UPDATE equipe_externa.tbl_detalhes_relatorio
            SET tipo_detalhe = @tipo_detalhe, valor_detalhe = @valor_detalhe,
                data = @data, descricao = @descricao, data_pagto = @data_pagto,
                numero_nf = @numero_nf, empresa_pagadora = @empresa_pagadora,
                alterado_por = @alterado_por, alterado_em = @alterado_em
            WHERE cod_detalhe_relatorio = @cod_detalhe_relatorio AND codrelatorio = @codrelatorio
              AND NOT COALESCE(envia_fluxo, false);", model, transaction) != 1)
            throw new InvalidOperationException("Nota excluida, movida ou enviada ao fluxo. Recarregue a tela.");

        await connection.ExecuteAsync("SELECT equipe_externa.atualizar_saldo_relatorio(@codrelatorio);",
            new { model.codrelatorio }, transaction);
        await transaction.CommitAsync();
        model.cod_detalhe_relatorio = id;
        return true;
    }
}
