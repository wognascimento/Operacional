using Npgsql;
using Operacional.DataBase;
using System.Data;
using System.IO;

namespace Operacional.Utils;

internal static class CartaApoioMontagemService
{
    private const string NomePlanilha = "qrybasemalaapoio";
    private const string Consulta = "SELECT * FROM operacional.qrybasemalaapoio;";

    internal sealed record Resultado(string Carta, string Planilha, int Registros);

    public static async Task<Resultado?> GerarAsync(CancellationToken cancellationToken)
    {
        var settings = DataBaseSettings.Instance;
        var modelo = SistemaPathResolver.GetModeloPath("MODELO_CARTA_APOIO_MONTAGEM.docx");
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
            $"CartaApoioMontagem_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pasta);
        var carta = Path.Combine(pasta, Path.GetFileName(modelo));
        var planilha = Path.Combine(pasta, "MALA_APOIO.xlsx");
        var assunto = $"EQUIPAMENTO DE APOIO PARA A MONTAGEM DECORAÇÃO DE NATAL {settings.Database}";

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                MalaDiretaMontagem.ExportarExcel(dados, planilha, cancellationToken, NomePlanilha);
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(modelo, carta);
                MalaDiretaMontagem.VincularPlanilha(carta, planilha, NomePlanilha);
                MalaDiretaMontagem.ConfigurarAssuntoEmail(carta, assunto);
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
