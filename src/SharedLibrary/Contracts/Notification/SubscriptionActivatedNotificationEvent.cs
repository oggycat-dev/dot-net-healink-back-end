using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Notification;

/// <summary>
/// Notification event when subscription is activated
/// Consumed by NotificationService to send email/SMS to user
/// This is a fire-and-forget event - no response expected
/// </summary>
public record SubscriptionActivatedNotificationEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    
    /// <summary>
    /// UserId for querying cache to get email (from UpdatedBy in ActivateSubscription command)
    /// </summary>
    public Guid UserId { get; init; }
    
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string SubscriptionPlanDisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime ActivatedAt { get; init; }
    
    // Payment info for notification
    public string PaymentProvider { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
}

