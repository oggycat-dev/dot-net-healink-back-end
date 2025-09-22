using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Event when OTP is verified successfully
/// </summary>
public record OtpVerified : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Contact { get; init; } = string.Empty;
    public OtpTypeEnum Type { get; init; } = OtpTypeEnum.Registration;
    public DateTime VerifiedAt { get; init; } = DateTime.UtcNow;
    
    public OtpVerified() : base("AuthService") { }
}