using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Auth;

public record ResetPasswordEvent : IntegrationEvent
{
    public string Contact { get; init; } = string.Empty; // email or phone number
    public NotificationChannelEnum OtpSentChannel { get; init; }
    public string Otp { get; init; } = string.Empty; // OTP code
    public int ExpiresInMinutes { get; init; }
    public ResetPasswordEvent() : base("AuthService") { }
}