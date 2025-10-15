using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Contracts.Subscription.Events;

namespace ContentService.Infrastructure.Consumers;

/// <summary>
/// Consumer to handle user subscription status changes and update Redis cache
/// This allows other services to quickly check user subscription status without querying database
/// </summary>
public class UserSubscriptionStatusChangedConsumer : IConsumer<UserSubscriptionStatusChangedEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserSubscriptionStatusChangedConsumer> _logger;

    public UserSubscriptionStatusChangedConsumer(
        IUserStateCache userStateCache,
        ILogger<UserSubscriptionStatusChangedConsumer> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserSubscriptionStatusChangedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing subscription status change for UserId={UserId}, SubscriptionId={SubscriptionId}, Action={Action}, Status={Status}",
            message.UserId, message.SubscriptionId, message.Action, message.SubscriptionStatus);

        try
        {
            // Build subscription info from event
            var subscriptionInfo = new UserSubscriptionInfo
            {
                SubscriptionId = message.SubscriptionId,
                SubscriptionPlanId = message.SubscriptionPlanId,
                SubscriptionPlanName = message.SubscriptionPlanName,
                SubscriptionPlanDisplayName = message.SubscriptionPlanDisplayName,
                SubscriptionStatus = message.SubscriptionStatus,
                CurrentPeriodStart = message.CurrentPeriodStart,
                CurrentPeriodEnd = message.CurrentPeriodEnd,
                ActivatedAt = message.ActivatedAt,
                CanceledAt = message.CanceledAt
            };

            // Update cache
            await _userStateCache.UpdateUserSubscriptionAsync(message.UserId, subscriptionInfo);

            _logger.LogInformation(
                "Successfully cached subscription status for UserId={UserId}: SubscriptionId={SubscriptionId}, Status={Status}, Plan={Plan}",
                message.UserId, message.SubscriptionId, message.SubscriptionStatus, message.SubscriptionPlanName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error caching subscription status for UserId={UserId}, SubscriptionId={SubscriptionId}",
                message.UserId, message.SubscriptionId);
            
            // Don't throw - this is a cache update, not critical for business logic
            // The system can still function if cache update fails
        }
    }
}

