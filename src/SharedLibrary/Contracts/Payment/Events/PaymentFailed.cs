using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Payment.Events;

/// <summary>
/// Event published by PaymentService when payment processing fails
/// Inherits from IntegrationEvent to capture IP/UserAgent for activity logging
/// </summary>
public record PaymentFailed : IntegrationEvent
{
    public Guid PaymentIntentId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    
    public PaymentFailed() : base("PaymentService") { }
}

