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
