using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

public record SubscriptionCanceledEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public bool CancelAtPeriodEnd { get; init; }
    public DateTime? CancelAt { get; init; }
    public DateTime? CanceledAt { get; init; }
    public string? Reason { get; init; }
    public Guid? CanceledBy { get; init; }
}
