using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Operacional.Views.EquipeExterna;

internal sealed class ClienteAplicativo
{
    public long id_aprovado { get; set; }
    public string nome { get; set; } = "";
    public string sigla { get; set; } = "";
    public bool Novo { get; set; }
    public string ChaveApi { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Raio { get; set; } = 200;
}

internal sealed class ClientesAplicativoService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.cipolatti.com.br/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static string NormalizarId(string id)
    {
        return long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero)
            ? numero.ToString(CultureInfo.InvariantCulture) : id.Trim();
    }

    internal static List<ClienteAplicativo> PrepararSincronizacao(IEnumerable<ClienteAplicativo> origem, string json)
    {
        var remotos = new Dictionary<string, JToken>(StringComparer.Ordinal);
        foreach (var remoto in JArray.Parse(json))
        {
            var chave = remoto["id_aprovado"]?.ToString();
            if (string.IsNullOrWhiteSpace(chave) || !remotos.TryAdd(NormalizarId(chave), remoto))
                throw new InvalidOperationException("A API retornou clientes com codigo vazio ou duplicado. Sincronizacao interrompida.");
        }
        var resultado = new List<ClienteAplicativo>();
        foreach (var cliente in origem)
        {
            cliente.ChaveApi = cliente.id_aprovado.ToString(CultureInfo.InvariantCulture);
            cliente.Novo = !remotos.TryGetValue(cliente.ChaveApi, out var remoto);
            if (!cliente.Novo)
            {
                if (cliente.nome == remoto!["nome"]?.ToString() && cliente.sigla == remoto["sigla"]?.ToString())
                    continue;
                cliente.ChaveApi = remoto!["id_aprovado"]!.ToString();
                var latitude = remoto["geolocalizacao"]?["latitude"] ?? remoto["latitude"];
                var longitude = remoto["geolocalizacao"]?["longitude"] ?? remoto["longitude"];
                cliente.Latitude = latitude?.Value<double?>() ?? throw new InvalidOperationException("Latitude ausente para " + cliente.sigla);
                cliente.Longitude = longitude?.Value<double?>() ?? throw new InvalidOperationException("Longitude ausente para " + cliente.sigla);
                cliente.Raio = remoto["raio"]?.Value<double?>() ?? throw new InvalidOperationException("Raio ausente para " + cliente.sigla);
            }
            else
            {
                cliente.Latitude = 0;
                cliente.Longitude = 0;
                cliente.Raio = 200;
            }
            resultado.Add(cliente);
        }
        return resultado;
    }

    public async Task<List<ClienteAplicativo>> ConsultarPendentesAsync(string connectionString)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/clientes");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        var clientes = (await connection.QueryAsync<ClienteAplicativo>(@"
            SELECT id_aprovado, nome, sigla_serv AS sigla
            FROM producao.t_aprovados
            ORDER BY id_aprovado ASC;")).ToList();
        if (clientes.Any(c => string.IsNullOrWhiteSpace(c.nome) || string.IsNullOrWhiteSpace(c.sigla)))
            throw new InvalidOperationException("Existem novos clientes sem nome ou sigla. Corrija o cadastro antes de enviar.");
        return PrepararSincronizacao(clientes, json);
    }

    public async Task EnviarAsync(ClienteAplicativo cliente)
    {
        // Initial coordinates are corrected by the team using the reported client list.
        var payload = new
        {
            id_aprovado = cliente.ChaveApi,
            cliente.nome,
            cliente.sigla,
            latitude = cliente.Latitude,
            longitude = cliente.Longitude,
            raio = cliente.Raio
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/clientes");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"A API recusou o cliente {cliente.sigla} (HTTP {(int)response.StatusCode}). Verifique os dados do cadastro.");
    }

    public async Task<IReadOnlyList<string>> ConsultarIdsEquipeAsync(string connectionString, long id_equipe)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        var aprovados = (await connection.QueryAsync<long?>(@"
            SELECT DISTINCT id_aprovado
            FROM equipe_externa.qry_previsao_valores_cronograma
            WHERE id_equipe = @id_equipe
            ORDER BY id_aprovado;", new { id_equipe })).ToList();
        if (aprovados.Any(id => !id.HasValue || id.Value <= 0))
            throw new InvalidOperationException("A equipe possui cliente sem id_aprovado valido. Corrija a origem antes de enviar.");

        // Refresh after client creation to obtain the API's internal identifiers.
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/clientes");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return ResolverIdsEquipe(await response.Content.ReadAsStringAsync(), aprovados.Select(id => id!.Value));
    }

    internal static IReadOnlyList<string> ResolverIdsEquipe(string json, IEnumerable<long> aprovados)
    {
        var clientes = JArray.Parse(json);
        var ids = new List<string>();
        var ausentes = new List<long>();
        foreach (var aprovado in aprovados.Distinct())
        {
            var correspondencias = clientes.Where(c => long.TryParse(c["id_aprovado"]?.ToString(),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero) && numero == aprovado).ToList();
            if (correspondencias.Count == 0)
            {
                ausentes.Add(aprovado);
                continue;
            }
            var id = correspondencias[0]["id"]?.ToString();
            if (correspondencias.Count != 1 || string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException($"Cliente {aprovado} com correspondencia duplicada ou ID interno vazio na API.");
            ids.Add(id);
        }
        if (ausentes.Count > 0)
            throw new InvalidOperationException("Clientes da equipe nao encontrados na API (id_aprovado): " +
                string.Join(", ", ausentes) + ". Nenhuma lista parcial de vinculos foi preparada.");
        return ids.Distinct().ToList();
    }
}
