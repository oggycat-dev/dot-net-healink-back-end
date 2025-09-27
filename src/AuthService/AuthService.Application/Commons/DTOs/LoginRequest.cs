using System.Text.Json.Serialization;
using AuthService.Application.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public GrantTypeEnum GrantType { get; set; } = GrantTypeEnum.Password;

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }
}