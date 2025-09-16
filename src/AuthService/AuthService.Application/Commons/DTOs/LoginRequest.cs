using System.Text.Json.Serialization;
using AuthService.Application.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("grant_type")]
    public GrantTypeEnum GrantType { get; set; } = GrantTypeEnum.Password;

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }
}