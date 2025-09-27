using System.Text.Json.Serialization;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.DTOs;

public class VerifyOtpRequest
{
    public string Contact { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public NotificationChannelEnum OtpSentChannel { get; init; } = NotificationChannelEnum.Email;
    public OtpTypeEnum OtpType { get; init; }
}