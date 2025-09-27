using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class ResetPasswordRequest
{
    public string Contact { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public NotificationChannelEnum OtpSentChannel { get; init; } 
}