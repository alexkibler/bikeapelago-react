using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Models;

public class LoginRequest
{
    [JsonPropertyName("identity")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
