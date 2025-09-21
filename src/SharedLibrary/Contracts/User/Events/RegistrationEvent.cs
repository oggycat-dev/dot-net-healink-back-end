using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Events;

public record RegistrationEvent : IntegrationEvent
{
    public string OtpCode { get; init; } = null!;
    public NotificationChannelEnum OtpChannel { get; init; }
    public int ExpiresInMinutes { get; init; }
    public OtpTypeEnum OtpType { get; init; } = OtpTypeEnum.Registration;
    public string Email { get; init; } = null!;
    public string EncryptedPassword { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public RegistrationEvent() : base("AuthService") { }
}

/// <summary>
/// Event để generate OTP cho user registration
/// </summary>
public record GenerateOtp
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Contact { get; init; } = string.Empty; // Email hoặc Phone
    public NotificationChannelEnum Channel { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public OtpTypeEnum OtpType { get; init; }
    public int ExpiryMinutes { get; init; } = 5;
}