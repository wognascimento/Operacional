using System.Text.Json.Serialization;

namespace Operacional.DataBase.Models.DTOs.Api;
/*
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
*/

public class ApiResponse<T>
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; }
}

public class ClienteFaseDto
{
    public int id_aprovado { get; set; }
    public string sigla_serv { get; set; }
    public DateTime data_inicio { get; set; }
    public DateTime data_fim { get; set; }
    public string fase { get; set; }
    public int id_user { get; set; } = 1;
}

public class LiberacaoEquipeDto
{
    public int id_user { get; set; } = 1;
    public int id_aprovado { get; set; }
    public string sigla_serv { get; set; }
    public string fase { get; set; }
    public string funcao { get; set; }
    public int qtd_pessoas { get; set; }
    public decimal valor_ano_atual { get; set; }
    public decimal lanche { get; set; }
    public decimal transporte { get; set; }
}

public class LiberacaoManutencaoEquipeDto
{
    public int id_user { get; set; } = 1;
    public int id_aprovado { get; set; }
    public string sigla_serv { get; set; }
    public string fase { get; set; }
    public string funcao { get; set; }
    public int qtd_pessoas { get; set; }
    public decimal valor_ano_atual { get; set; }
    public decimal lanche { get; set; }
    public decimal transporte { get; set; }
    public DateTime data { get; set; }
}

public class BulkPayload
{
    public List<ClienteFaseDto> clientes_fase { get; set; }
    public List<LiberacaoEquipeDto> liberacao_equipe { get; set; }
    public List<LiberacaoManutencaoEquipeDto> liberacao_manutencao_equipe { get; set; }
}

public class EquipeLancamentoDto
{
    //[JsonPropertyName("id")]
    public int id { get; set; }

    //[JsonPropertyName("id_user")]
    public int id_user { get; set; }

    public long id_equipe { get; set; }

    //[JsonPropertyName("id_aprovado")]
    public int? id_aprovado { get; set; }

    //[JsonPropertyName("fase")]
    public string fase { get; set; }

    //[JsonPropertyName("funcao")]
    public string funcao { get; set; }

    //[JsonPropertyName("data")]
    public DateTimeOffset? data { get; set; }

    //[JsonPropertyName("pessoas")]
    public int pessoas { get; set; }

    //[JsonPropertyName("editado_usuario")]
    public bool editado_usuario { get; set; }

    //[JsonPropertyName("extra")]
    public bool extra { get; set; }

    //[JsonPropertyName("created_at")]
    public DateTimeOffset? created_at { get; set; }

    //[JsonPropertyName("updated_at")]
    public DateTimeOffset? updated_at { get; set; }
}