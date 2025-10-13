namespace SubscriptionService.Application.Features.Subscriptions.HandleSubscriptionSagaCommand;

/// <summary>
/// Response data for subscription saga activation
/// Used to pass data from handler to consumer without re-querying DB
/// </summary>
public class SubscriptionSagaResponse
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string SubscriptionPlanDisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime ActivatedAt { get; init; }
    public DateTime? CurrentPeriodStart { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
}

