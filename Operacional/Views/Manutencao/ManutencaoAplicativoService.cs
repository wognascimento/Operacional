using Dapper;
using Newtonsoft.Json;
using Npgsql;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace Operacional.Views.Manutencao;

internal sealed class ManutencaoAplicativoService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };
    internal sealed class Linha
    {
        public string? id_aprovado { get; set; }
        public string? fase { get; set; }
        public DateTime? data_inicio { get; set; }
        public DateTime? data_fim { get; set; }
        public string? funcao { get; set; }
        public decimal? limite_integrantes { get; set; }
    }
    internal static string MontarJson(IEnumerable<Linha> origem)
    {
        var linhas = origem.ToList();
        var invalidas = linhas.Where(x => string.IsNullOrWhiteSpace(x.id_aprovado) ||
            string.IsNullOrWhiteSpace(x.fase) || !x.data_inicio.HasValue || !x.data_fim.HasValue ||
            x.data_fim < x.data_inicio || string.IsNullOrWhiteSpace(x.funcao) ||
            !x.limite_integrantes.HasValue || x.limite_integrantes < 0 || x.limite_integrantes > int.MaxValue ||
            decimal.Truncate(x.limite_integrantes.Value) != x.limite_integrantes).ToList();
        if (invalidas.Count > 0)
            throw new InvalidOperationException("Manutenções com dados inválidos. Clientes: " +
                string.Join(", ", invalidas.Select(x => x.id_aprovado ?? "(sem ID)").Distinct()));
        var fases = linhas.GroupBy(x => new { x.id_aprovado, x.fase, Inicio = x.data_inicio!.Value.Date, Fim = x.data_fim!.Value.Date })
            .Select(g => new
            {
                g.Key.id_aprovado, g.Key.fase,
                data_inicio = g.Key.Inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                data_fim = g.Key.Fim.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                funcoes = g.GroupBy(x => x.funcao).Select(f =>
                {
                    var valores = f.Select(x => (int)x.limite_integrantes!.Value).Distinct().ToArray();
                    if (valores.Length != 1)
                        throw new InvalidOperationException($"Quantidades divergentes: cliente {g.Key.id_aprovado}, {g.Key.Inicio:dd/MM/yyyy}, função {f.Key}.");
                    return new { funcao = f.Key, limite_integrantes = valores[0] };
                }).ToArray()
            }).ToArray();
        return JsonConvert.SerializeObject(new { clientes = Array.Empty<object>(), cronograma_fases = fases });
    }
    internal async Task EnviarAsync(string connectionString)
    {
        await using var db = new NpgsqlConnection(connectionString);
        var linhas = (await db.QueryAsync<Linha>("""
            SELECT id_aprovado::text AS id_aprovado, fase,
                   data::timestamp AS data_inicio, data::timestamp AS data_fim,
                   funcao, qtd_pessoas AS limite_integrantes
            FROM equipe_externa.qry_funcoes_equipe_usuario_manutencao
            """)).ToList();
        var json = MontarJson(linhas);
        if (linhas.Count == 0) return;
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cipolatti.com.br/api/cronograma/importar");
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"API recusou as manutenções (HTTP {(int)response.StatusCode}). Confira o resultado na API antes de repetir.");
    }
}
