using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Command to send OTP notification
/// </summary>
public record SendOtpNotification : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Contact { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public NotificationChannelEnum Channel { get; init; }
    public OtpTypeEnum OtpType { get; init; } = OtpTypeEnum.Registration;
    public string FullName { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; }
    
    public SendOtpNotification() : base("RegistrationSaga") { }
}

/// <summary>
/// Response event after OTP notification sent
/// </summary>
public record OtpSent : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
    
    public OtpSent() : base("NotificationService") { }
}