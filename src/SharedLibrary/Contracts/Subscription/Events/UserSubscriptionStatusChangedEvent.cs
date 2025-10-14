using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Events;

/// <summary>
/// Event published when user subscription status changes (activated, canceled, expired)
/// Used to update user subscription status in Redis cache for quick access across services
/// </summary>
public record UserSubscriptionStatusChangedEvent : IntegrationEvent
{
    /// <summary>
    /// User ID (authUserId from JWT - for cache key)
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// UserProfile ID (business entity)
    /// </summary>
    public Guid UserProfileId { get; init; }
    
    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid SubscriptionId { get; init; }
    
    /// <summary>
    /// Subscription Plan ID
    /// </summary>
    public Guid SubscriptionPlanId { get; init; }
    
    /// <summary>
    /// Subscription Plan Name (technical name)
    /// </summary>
    public string SubscriptionPlanName { get; init; } = string.Empty;
    
    /// <summary>
    /// Subscription Plan Display Name (user-friendly name)
    /// </summary>
    public string SubscriptionPlanDisplayName { get; init; } = string.Empty;
    
    /// <summary>
    /// Subscription Status: 0=Pending, 1=Active, 2=Expired, 3=Canceled
    /// </summary>
    public int SubscriptionStatus { get; init; }
    
    /// <summary>
    /// Current billing period start date (null if not active)
    /// </summary>
    public DateTime? CurrentPeriodStart { get; init; }
    
    /// <summary>
    /// Current billing period end date (null if not active)
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; init; }
    
    /// <summary>
    /// When subscription was activated (null if never activated)
    /// </summary>
    public DateTime? ActivatedAt { get; init; }
    
    /// <summary>
    /// When subscription was canceled (null if not canceled)
    /// </summary>
    public DateTime? CanceledAt { get; init; }
    
    /// <summary>
    /// Action that triggered this event: "Activated", "Canceled", "Expired"
    /// </summary>
    public string Action { get; init; } = string.Empty;
}

