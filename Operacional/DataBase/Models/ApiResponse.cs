using Newtonsoft.Json;

namespace Operacional.DataBase.Models;

public class ApiResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("usuario")]
    public Usuario Usuario { get; set; }
}

public class Usuario
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("must_change_password")]
    public bool MustChangePassword { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }
}

public class ErrorResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }
}
