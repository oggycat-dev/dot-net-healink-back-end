using MediatR;
using SharedLibrary.Commons.Models;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpgradeSubscription;

/// <summary>
/// Command to upgrade/downgrade existing subscription to a different plan
/// Handles proration, cancellation of old subscription, and activation of new one
/// </summary>
public record UpgradeSubscriptionCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the new subscription plan to upgrade/downgrade to
    /// </summary>
    public Guid NewSubscriptionPlanId { get; init; }
    
    /// <summary>
    /// Whether to apply proration credit from old subscription
    /// Default: true (recommended)
    /// </summary>
    public bool ApplyProration { get; init; } = true;
    
    /// <summary>
    /// Whether to cancel old subscription immediately or at period end
    /// Default: false (cancel immediately)
    /// </summary>
    public bool CancelAtPeriodEnd { get; init; } = false;
}

