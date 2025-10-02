using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

/// <summary>
/// Event được publish khi tạo mới Subscription Plan
/// Kế thừa từ SubscriptionPlanEventBase để tái sử dụng properties chung
/// </summary>
public record SubscriptionPlanCreatedEvent : SubscriptionPlanEventBase
{
    // Additional fields specific to Create event
    public string Currency { get; init; } = string.Empty;
    public int BillingPeriodCount { get; init; }
    public string BillingPeriodUnit { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int TrialDays { get; init; }
    public Guid? CreatedBy { get; init; }
}
