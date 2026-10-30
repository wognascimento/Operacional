using Npgsql;
using Operacional.DataBase;
using System.Data;
using System.IO;

namespace Operacional.Utils;

internal static class CartaManutencaoService
{
    private const string NomePlanilha = "mala_manutencao";
    private const string Consulta = """
        SELECT DISTINCT v.x, c.sigla, r.contato, r.email, c.nome,
            c.ddd, c.fone1, c.fax, c.cidade, c.est
        FROM comercial.clientes AS c
        INNER JOIN operacional.view_calman AS v ON c.sigla = v.sigla
        LEFT JOIN operacional.qryrespdec AS r ON c.sigla = r.sigla
        WHERE v.x IS NOT NULL AND v.x <> ''
        ORDER BY c.sigla;
        """;

    internal sealed record Resultado(string Carta, string Planilha, int Registros);

    public static async Task<Resultado?> GerarAsync(CancellationToken cancellationToken)
    {
        var settings = DataBaseSettings.Instance;
        var modelo = SistemaPathResolver.GetModeloPath("MODELO_MANUTENÇÃO.docx");
        using var dados = new DataTable(NomePlanilha);

        await using (var connection = new NpgsqlConnection(settings.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(Consulta, connection) { CommandTimeout = 120 };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            for (var i = 0; i < reader.FieldCount; i++)
                dados.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            while (await reader.ReadAsync(cancellationToken))
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                dados.Rows.Add(values);
            }
        }

        if (dados.Rows.Count == 0)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var pasta = SistemaPathResolver.GetImpressosPath(
            $"CartaManutencao_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pasta);
        var carta = Path.Combine(pasta, Path.GetFileName(modelo));
        var planilha = Path.Combine(pasta, "MALA_MANUTENCAO.xlsx");


        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                MalaDiretaMontagem.ExportarExcel(dados, planilha, cancellationToken, NomePlanilha);
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(modelo, carta);
                MalaDiretaMontagem.VincularPlanilha(carta, planilha, NomePlanilha);

            }, cancellationToken);
        }
        catch
        {
            foreach (var path in new[] { carta, planilha })
                try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { Directory.Delete(pasta); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }

        return new Resultado(carta, planilha, dados.Rows.Count);
    }
}
