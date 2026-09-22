using Npgsql;
using NpgsqlTypes;
using Operacional.DataBase;
using System.Data;
using System.IO;

namespace Operacional.Utils;

internal static class CartaTransporteMontagemService
{
    private const string Consulta = @"
        SELECT *
        FROM operacional.qrybasemalamontagem
        WHERE mindedata_de_expedicao >= @inicio
          AND mindedata_de_expedicao < @fimExclusivo
          AND transporte = 'CLIENTE'
        ORDER BY mindedata_de_expedicao;";

    internal sealed record Resultado(string Carta, string Planilha, int Registros);

    public static async Task<Resultado?> GerarAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        if (inicio.Date > fim.Date || fim.Date == DateTime.MaxValue.Date)
            throw new ArgumentException("Periodo invalido.");

        var settings = DataBaseSettings.Instance;
        var modelo = SistemaPathResolver.GetModeloPath("MODELO_CARTA_TRANSPORTE_MONTAGEM.docx");
        using var dados = new DataTable(MalaDiretaMontagem.NomePlanilha);

        await using (var connection = new NpgsqlConnection(settings.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(Consulta, connection)
            {
                CommandTimeout = 120
            };
            command.Parameters.AddWithValue("inicio", NpgsqlDbType.Date, DateOnly.FromDateTime(inicio));
            command.Parameters.AddWithValue("fimExclusivo", NpgsqlDbType.Date, DateOnly.FromDateTime(fim).AddDays(1));
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
            $"CartaTransporteMontagem_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pasta);
        var carta = Path.Combine(pasta, Path.GetFileName(modelo));
        var planilha = Path.Combine(pasta, "MALA_MONTAGEM.xlsx");
        var assunto = $"TRANSPORTE PARA A MONTAGEM DECORAÇÃO DE NATAL {settings.Database}";

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                MalaDiretaMontagem.ExportarExcel(dados, planilha, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(modelo, carta);
                MalaDiretaMontagem.VincularPlanilha(carta, planilha);
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
