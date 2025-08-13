using System.Text.Json.Serialization;

namespace Operacional.DataBase.Models.DTOs.Api;

public class ClienteFaseDto
{
    [JsonPropertyName("id_aprovado")]
    public int IdAprovado { get; set; }

    [JsonPropertyName("sigla_serv")]
    public string SiglaServ { get; set; }

    // Use DateTime? para aceitar null; serializa ISO 8601 por padrão
    [JsonPropertyName("data_inicio")]
    public DateTime? DataInicio { get; set; }

    [JsonPropertyName("data_fim")]
    public DateTime? DataFim { get; set; }

    [JsonPropertyName("fase")]
    public string Fase { get; set; }

    [JsonPropertyName("id_user")]
    public int IdUser { get; set; }
}

public class BulkRequest
{
    [JsonPropertyName("items")]
    public List<ClienteFaseDto> Items { get; set; }
}
