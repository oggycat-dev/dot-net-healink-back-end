using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Commands;

/// <summary>
/// Command to cancel/rollback subscription (compensation action)
/// Used by saga when payment fails to cleanup partial subscription
/// </summary>
public record CancelSubscription : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool IsCompensation { get; init; } = false; // True when called from saga rollback
}

