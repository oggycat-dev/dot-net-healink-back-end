using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Requests;

/// <summary>
/// Request to get user subscription by UserProfileId
/// Used for RPC calls from AuthService to SubscriptionService
/// </summary>
public record GetUserSubscriptionRequest : IntegrationEvent
{
    /// <summary>
    /// UserProfileId (business entity ID)
    /// </summary>
    public Guid UserProfileId { get; init; }

    public GetUserSubscriptionRequest() : base("AuthService") { }
}

/// <summary>
/// Response containing user subscription data
/// </summary>
public record GetUserSubscriptionResponse : IntegrationEvent
{
    /// <summary>
    /// Whether subscription was found
    /// </summary>
    public bool Found { get; init; }

    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid? SubscriptionId { get; init; }

    /// <summary>
    /// Subscription Plan ID
    /// </summary>
    public Guid? SubscriptionPlanId { get; init; }

    /// <summary>
    /// Subscription Plan Name (technical name)
    /// </summary>
    public string? SubscriptionPlanName { get; init; }

    /// <summary>
    /// Subscription Plan Display Name (user-friendly name)
    /// </summary>
    public string? SubscriptionPlanDisplayName { get; init; }

    /// <summary>
    /// Subscription Status: 0=Pending, 1=Active, 2=Expired, 3=Canceled
    /// </summary>
    public int? SubscriptionStatus { get; init; }

    /// <summary>
    /// Current billing period start date
    /// </summary>
    public DateTime? CurrentPeriodStart { get; init; }

    /// <summary>
    /// Current billing period end date
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; init; }

    /// <summary>
    /// When subscription was canceled
    /// </summary>
    public DateTime? CanceledAt { get; init; }

    /// <summary>
    /// When subscription was activated
    /// </summary>
    public DateTime? ActivatedAt { get; init; }

    public GetUserSubscriptionResponse() : base("SubscriptionService") { }
}
