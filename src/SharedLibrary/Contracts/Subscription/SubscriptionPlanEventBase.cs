using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

/// <summary>
/// Base event cho tất cả Subscription Plan events - tái sử dụng properties chung
/// </summary>
public abstract record SubscriptionPlanEventBase : IntegrationEvent
{
    public Guid SubscriptionPlanId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FeatureConfig { get; init; } = "{}";
}
