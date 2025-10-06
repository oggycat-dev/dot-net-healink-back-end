using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

/// <summary>
/// Event được publish khi cập nhật Subscription Plan
/// Kế thừa từ SubscriptionPlanEventBase để tái sử dụng properties chung
/// </summary>
public record SubscriptionPlanUpdatedEvent : SubscriptionPlanEventBase
{
    // Additional fields specific to Update event
    public decimal Amount { get; init; }
    public int TrialDays { get; init; }
    public int BillingPeriodCount { get; init; }
    public Guid? UpdatedBy { get; init; }
}
