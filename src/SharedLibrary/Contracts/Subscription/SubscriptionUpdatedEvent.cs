using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription;

public record SubscriptionUpdatedEvent : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string SubscriptionStatus { get; init; } = string.Empty;
    public string RenewalBehavior { get; init; } = string.Empty;
    public bool CancelAtPeriodEnd { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public Guid? UpdatedBy { get; init; }
}
