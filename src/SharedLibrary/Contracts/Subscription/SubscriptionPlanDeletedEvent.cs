using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

/// <summary>
/// Event được publish khi xóa (soft delete) Subscription Plan
/// Kế thừa từ SubscriptionPlanEventBase để tái sử dụng properties chung
/// </summary>
public record SubscriptionPlanDeletedEvent : SubscriptionPlanEventBase
{
    // Additional fields specific to Delete event
    public Guid? DeletedBy { get; init; }
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
}
