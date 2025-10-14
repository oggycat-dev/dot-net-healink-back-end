using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Payment.Events;

/// <summary>
/// Event published by PaymentService when payment is successfully processed
/// Inherits from IntegrationEvent to capture IP/UserAgent for activity logging
/// </summary>
public record PaymentSucceeded : IntegrationEvent
{
    public Guid PaymentIntentId { get; init; }
    public Guid SubscriptionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string PaymentProvider { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public DateTime PaidAt { get; init; } = DateTime.UtcNow;
    
    public PaymentSucceeded() : base("PaymentService") { }
}

