using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public NotificationChannelEnum? OtpSentChannel { get; set; } = NotificationChannelEnum.Email;
}