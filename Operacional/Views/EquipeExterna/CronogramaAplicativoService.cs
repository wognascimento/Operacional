using Dapper;
using Newtonsoft.Json;
using Npgsql;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Operacional.Views.EquipeExterna;

internal sealed class CronogramaAplicativoService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    internal sealed class Fase
    {
        public string id_aprovado { get; set; } = "";
        public string fase { get; set; } = "";
        public DateTime? data_inicio { get; set; }
        public DateTime? data_fim { get; set; }
    }

    internal sealed class Funcao
    {
        public string id_aprovado { get; set; } = "";
        public string fase { get; set; } = "";
        public string funcao { get; set; } = "";
        public decimal? limite_integrantes { get; set; }
    }

    public async Task<string> PrepararAsync(string? connectionString, long? idEquipe)
    {
        if (idEquipe is null or <= 0) throw new InvalidOperationException("Informe e salve a equipe do usuario.");
        await using var db = new NpgsqlConnection(connectionString);
        var fases = (await db.QueryAsync<Fase>("""
            SELECT q.id_aprovado::text, q.fase,
                CASE WHEN q.fase='MONTAGEM' THEN d.data_inicio_montagem ELSE d.data_inicio_desmontagem END::timestamp AS data_inicio,
                (CASE WHEN q.fase='MONTAGEM' THEN d.data_inicio_montagem ELSE d.data_inicio_desmontagem END
                    + max(q.qtd_noites)::integer)::timestamp AS data_fim
            FROM equipe_externa.qry_previsao_valores_cronograma q
            INNER JOIN operacional.t_data_efetiva d ON q.sigla=d.siglaserv
            WHERE q.fase IN ('MONTAGEM','DESMONTAGEM') AND q.id_equipe=@idEquipe
            GROUP BY q.id_aprovado,q.sigla,q.fase,d.data_inicio_montagem,d.data_inicio_desmontagem
            ORDER BY q.id_aprovado,q.fase
            """, new { idEquipe })).ToList();
        var funcoes = (await db.QueryAsync<Funcao>("""
            SELECT DISTINCT id_aprovado::text, fase, funcao, qtd_pessoas AS limite_integrantes
            FROM equipe_externa.qry_previsao_valores_cronograma
            WHERE fase IN ('MONTAGEM','DESMONTAGEM') AND valor_ano_atual>0 AND id_equipe=@idEquipe
            ORDER BY id_aprovado,fase,funcao
            """, new { idEquipe })).ToList();
        if (fases.Any(f => string.IsNullOrWhiteSpace(f.id_aprovado) || !f.data_inicio.HasValue || !f.data_fim.HasValue || f.data_fim < f.data_inicio))
            throw new InvalidOperationException("Cronograma com cliente ou datas invalidas. Confira as datas efetivas e a quantidade de noites da equipe.");
        if (fases.GroupBy(f => (f.id_aprovado, f.fase)).Any(g => g.Select(f => (f.data_inicio, f.data_fim)).Distinct().Count() > 1))
            throw new InvalidOperationException("Existem datas diferentes para o mesmo cliente e fase. Corrija o cronograma antes de enviar.");
        if (funcoes.Any(f => string.IsNullOrWhiteSpace(f.funcao) || !f.limite_integrantes.HasValue || f.limite_integrantes < 0 || f.limite_integrantes > int.MaxValue || decimal.Truncate(f.limite_integrantes.Value) != f.limite_integrantes))
            throw new InvalidOperationException("Cronograma com funcao ou limite de integrantes invalido.");
        if (funcoes.GroupBy(f => (f.id_aprovado, f.fase, f.funcao)).Any(g => g.Select(f => f.limite_integrantes).Distinct().Count() > 1))
            throw new InvalidOperationException("Existem limites diferentes para a mesma funcao, cliente e fase. Corrija antes de enviar.");
        if (funcoes.Any(f => !fases.Any(p => p.id_aprovado == f.id_aprovado && p.fase == f.fase)))
            throw new InvalidOperationException("Existem funcoes sem datas efetivas para o cliente e fase.");
        return JsonConvert.SerializeObject(new
        {
            clientes = Array.Empty<object>(),
            cronograma_fases = fases.DistinctBy(f => (f.id_aprovado, f.fase)).Select(f => new
            {
                f.id_aprovado, f.fase,
                data_inicio = f.data_inicio!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                data_fim = f.data_fim!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                funcoes = funcoes.Where(p => p.id_aprovado == f.id_aprovado && p.fase == f.fase)
                    .Select(p => new { p.funcao, limite_integrantes = (int)p.limite_integrantes!.Value })
            })
        });
    }

    public async Task EnviarAsync(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cipolatti.com.br/api/cronograma/importar");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Importacao do cronograma recusada pela API (HTTP {(int)response.StatusCode}). Confira o resultado na API antes de reenviar.");
    }
}
