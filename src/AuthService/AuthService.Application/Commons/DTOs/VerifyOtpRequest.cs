using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class VerifyOtpRequest
{
    [JsonPropertyName("contact")]
    public string Contact { get; init; } = string.Empty;
    [JsonPropertyName("otp_code")]
    public string OtpCode { get; init; } = string.Empty;
    [JsonPropertyName("channel")]
    public NotificationChannelEnum Channel { get; init; } = NotificationChannelEnum.Email;
}