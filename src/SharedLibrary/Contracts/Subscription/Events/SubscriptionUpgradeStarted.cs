using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Events;

/// <summary>
/// Event published when user upgrades/downgrades their subscription
/// Triggers saga to handle payment for difference or issue credit
/// </summary>
public record SubscriptionUpgradeStarted : IntegrationEvent
{
    public Guid NewSubscriptionId { get; init; }
    public Guid OldSubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    
    // New Plan Info
    public Guid NewPlanId { get; init; }
    public string NewPlanName { get; init; } = string.Empty;
    
    // Old Plan Info
    public Guid OldPlanId { get; init; }
    public string OldPlanName { get; init; } = string.Empty;
    
    // Pricing
    public bool IsUpgrade { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal ProrationCredit { get; init; }
    public decimal AmountToCharge { get; init; } // Original - Proration
    public string Currency { get; init; } = "VND";
    
    public SubscriptionUpgradeStarted() : base("SubscriptionService") { }
}

