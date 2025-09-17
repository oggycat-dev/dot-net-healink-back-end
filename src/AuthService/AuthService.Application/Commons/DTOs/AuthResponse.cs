using System.Text.Json.Serialization;

namespace AuthService.Application.Commons.DTOs;

public class AuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new List<string>();
}