using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Event to start the registration saga
/// </summary>
public record RegistrationStarted : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string EncryptedPassword { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public NotificationChannelEnum Channel { get; init; }
    public int ExpiresInMinutes { get; init; }
    
    public RegistrationStarted() : base("AuthService") { }
}