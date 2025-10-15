using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Events;

/// <summary>
/// Activity event for user activity logging when subscription is registered
/// Published via Custom Outbox (legacy RabbitMQ EventBus) for immediate activity logging
/// Separate from SubscriptionRegistrationStarted which triggers saga workflow
/// </summary>
public record SubscriptionRegisteredActivityEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string SubscriptionPlanDisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    
    // Activity logging specific fields
    public string ActivityType { get; init; } = "SubscriptionRegistered";
    public string Description { get; init; } = string.Empty;
    
    // Audit fields from BaseEntity
    public DateTime? CreatedAt { get; init; }
    
    // Correlation for tracking across services
    public Guid CorrelationId { get; init; }
    
    public SubscriptionRegisteredActivityEvent() : base("SubscriptionService") { }
}

