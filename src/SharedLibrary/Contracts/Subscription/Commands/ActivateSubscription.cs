using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Commands;

/// <summary>
/// Command to activate subscription after successful payment
/// Consumed by SubscriptionService
/// </summary>
public record ActivateSubscription : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid PaymentIntentId { get; init; }
    public string PaymentProvider { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
}

