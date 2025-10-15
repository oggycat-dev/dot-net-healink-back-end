using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Notification;

/// <summary>
/// Notification event for subscription payment result
/// Consumed by NotificationService to send email/SMS to user
/// </summary>
public record SubscriptionPaymentNotificationEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string? UserPhoneNumber { get; init; }
    
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    
    public bool IsSuccess { get; init; }
    public string PaymentProvider { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public string? ErrorMessage { get; init; }
    
    public DateTime ProcessedAt { get; init; }
    
    public SubscriptionPaymentNotificationEvent() : base("SubscriptionService") { }
}

