using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Operacional.DataBase.Models.DTOs.Api;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Operacional.Views.EquipeExterna;

internal sealed class UsuarioAplicativoService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<string> CadastrarAsync(string nome, string email,
        IReadOnlyList<FuncaoAplicativoDto> funcoes, IReadOnlyList<string> clientes)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cipolatti.com.br/api/auth/register");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonConvert.SerializeObject(new
        {
            nome, email, funcoes, cliente_ids = clientes,
            url_android = "https://play.google.com/store/apps/details?id=br.com.cipolatti.exponto"
        }), Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.BadGateway)
            throw new InvalidOperationException("A API informou falha no envio do e-mail (HTTP 502). Conforme o contrato, o cadastro foi revertido.");
        if (!response.IsSuccessStatusCode)
        {
            string detalhe = "";
            try
            {
                var erro = JObject.Parse(body);
                detalhe = (erro["detail"] ?? erro["message"])?.ToString() ?? "";
            }
            catch (JsonException) { }
            if (detalhe.Length > 500) detalhe = detalhe[..500];
            throw new InvalidOperationException($"Cadastro recusado pela API (HTTP {(int)response.StatusCode}). {detalhe}");
        }
        var result = JObject.Parse(body);
        var id = result["id"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("A API aceitou a requisicao mas nao retornou o ID. Confira o cadastro antes de reenviar.");
        return id;
    }
}
