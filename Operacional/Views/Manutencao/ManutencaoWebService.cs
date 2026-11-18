using Dapper;
using Newtonsoft.Json;
using Npgsql;
using System.Net.Http;
using System.Text;

namespace Operacional.Views.Manutencao;

internal sealed class ManutencaoWebService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };
    internal sealed class Linha
    {
        public string? usuario_web { get; set; }
        public long? id_equipe { get; set; }
        public int? id_aprovado { get; set; }
        public string? sigla_serv { get; set; }
        public string? fase { get; set; }
        public string? funcao { get; set; }
        public decimal? qtd_pessoas { get; set; }
        public decimal? valor_ano_atual { get; set; }
        public decimal? lanche { get; set; }
        public decimal? transporte { get; set; }
        public DateTime? data { get; set; }
    }

    internal static string? MontarJson(IEnumerable<Linha> origem)
    {
        var linhas = origem.ToList();
        if (linhas.Count == 0) return null;
        var problemas = linhas.Where(x => !int.TryParse(x.usuario_web, out var id) || id <= 0 ||
            x.id_aprovado is null or <= 0 || string.IsNullOrWhiteSpace(x.sigla_serv) ||
            string.IsNullOrWhiteSpace(x.fase) || string.IsNullOrWhiteSpace(x.funcao) || !x.data.HasValue ||
            !x.qtd_pessoas.HasValue || x.qtd_pessoas < 0 || x.qtd_pessoas > int.MaxValue ||
            decimal.Truncate(x.qtd_pessoas.Value) != x.qtd_pessoas ||
            !x.valor_ano_atual.HasValue || !x.lanche.HasValue || !x.transporte.HasValue).ToList();
        if (problemas.Count > 0)
            throw new InvalidOperationException("Envio WEB: confira o cadastro do usuário WEB (campo aux), datas, funções, quantidades e valores.\n" +
                string.Join("\n", problemas.Select(x => $"Equipe {x.id_equipe}: {x.sigla_serv} - {x.data:dd/MM/yyyy}").Distinct()));

        var itens = linhas.Select(x => new
        {
            id_user = int.Parse(x.usuario_web!), id_aprovado = x.id_aprovado!.Value,
            x.sigla_serv, x.fase, x.funcao, qtd_pessoas = (int)x.qtd_pessoas!.Value,
            valor_ano_atual = x.valor_ano_atual!.Value, lanche = x.lanche!.Value,
            transporte = x.transporte!.Value, data = x.data!.Value.Date
        }).Distinct().ToArray();
        if (itens.GroupBy(x => new { x.id_user, x.id_aprovado, x.fase, x.funcao, x.data }).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Envio WEB: existem quantidades ou valores divergentes para a mesma manutenção, função e usuário.");
        // Não envie listas vazias de montagem/desmontagem: este fluxo altera somente manutenção.
        return JsonConvert.SerializeObject(new { liberacao_manutencao_equipe = itens });
    }

    internal async Task<string?> PrepararAsync(string connectionString)
    {
        await using var db = new NpgsqlConnection(connectionString);
        var linhas = await db.QueryAsync<Linha>("""
            SELECT u.aux::text AS usuario_web, q.id_equipe, q.id_aprovado,
                   q.sigla AS sigla_serv, q.fase, q.funcao, q.qtd_pessoas,
                   q.valor_ano_atual, q.lanche, q.transporte, q.data::timestamp AS data
            FROM equipe_externa.qry_funcoes_equipe_usuario_manutencao q
            LEFT JOIN equipe_externa.tblusuario u ON u.id_equipe = q.id_equipe
            WHERE u.aux IS NOT NULL
            """);
        return MontarJson(linhas);
    }

    internal async Task EnviarAsync(string? json)
    {
        if (json == null) return;
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://rest-api.cipolatti.com.br/api/bulk/all");
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Envio WEB recusado (HTTP {(int)response.StatusCode}). Confira o resultado na WEB antes de repetir.");
    }
}
