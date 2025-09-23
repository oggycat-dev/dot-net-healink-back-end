using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Event when registration saga completes successfully
/// </summary>
public record RegistrationCompleted : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    
    public RegistrationCompleted() : base("RegistrationSaga") { }
}

/// <summary>
/// Event when registration saga fails
/// </summary>
public record RegistrationFailed : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    
    public RegistrationFailed() : base("RegistrationSaga") { }
}