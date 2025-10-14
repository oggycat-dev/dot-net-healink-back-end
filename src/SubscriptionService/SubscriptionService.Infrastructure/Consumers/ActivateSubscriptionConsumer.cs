using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Notification;
using SharedLibrary.Contracts.Subscription.Commands;
using SharedLibrary.Contracts.Subscription.Events;
using SubscriptionService.Application.Features.Subscriptions.HandleSubscriptionSagaCommand;

namespace SubscriptionService.Infrastructure.Consumers;

/// <summary>
/// Consumer for ActivateSubscription command from saga
/// Delegates to CQRS command handler + publishes notification (fire-and-forget)
/// Uses data from handler result (no re-query needed)
/// </summary>
public class ActivateSubscriptionConsumer : IConsumer<ActivateSubscription>
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ActivateSubscriptionConsumer> _logger;

    public ActivateSubscriptionConsumer(
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        ILogger<ActivateSubscriptionConsumer> logger)
    {
        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ActivateSubscription> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received ActivateSubscription command: SubscriptionId={SubscriptionId}, PaymentIntentId={PaymentIntentId}",
            message.SubscriptionId, message.PaymentIntentId);

        // ✅ Step 1: Delegate to CQRS command handler (business logic)
        var command = new HandleSubscriptionSagaCommand
        {
            SubscriptionId = message.SubscriptionId,
            Action = SubscriptionSagaAction.Activate,
            PaymentIntentId = message.PaymentIntentId,
            PaymentProvider = message.PaymentProvider,
            TransactionId = message.TransactionId,
            UpdatedBy = message.UpdatedBy
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to activate subscription: SubscriptionId={SubscriptionId}, Error={Error}",
                message.SubscriptionId, result.Message);
            throw new Exception($"Failed to activate subscription: {result.Message}");
        }

        // ✅ Step 2: Publish events (fire-and-forget, infrastructure concern)
        // Use data from handler result (no DB re-query)
        try
        {
            if (result.Data != null)
            {
                // ✅ Cast Result.Data to SubscriptionSagaResponse
                var subscriptionData = (SubscriptionSagaResponse)result.Data;

                // ✅ Event 1: Notification (for email/SMS)
                var notificationEvent = new SubscriptionActivatedNotificationEvent
                {
                    SubscriptionId = subscriptionData.SubscriptionId,
                    UserProfileId = subscriptionData.UserProfileId,
                    UserId = message.UpdatedBy ?? Guid.Empty, // ✅ UserId from command UpdatedBy for cache query
                    SubscriptionPlanName = subscriptionData.SubscriptionPlanName,
                    SubscriptionPlanDisplayName = subscriptionData.SubscriptionPlanDisplayName,
                    Amount = subscriptionData.Amount,
                    Currency = subscriptionData.Currency,
                    ActivatedAt = subscriptionData.ActivatedAt,
                    PaymentProvider = message.PaymentProvider,
                    TransactionId = message.TransactionId
                };

                await _publishEndpoint.Publish(notificationEvent);

                _logger.LogInformation(
                    "Published notification event for SubscriptionId={SubscriptionId}",
                    message.SubscriptionId);

                // ✅ Event 2: Cache Update (for subscription status in Redis)
                var cacheEvent = new UserSubscriptionStatusChangedEvent
                {
                    UserId = message.UpdatedBy ?? Guid.Empty, // ✅ authUserId for cache key
                    UserProfileId = subscriptionData.UserProfileId,
                    SubscriptionId = subscriptionData.SubscriptionId,
                    SubscriptionPlanId = subscriptionData.SubscriptionPlanId,
                    SubscriptionPlanName = subscriptionData.SubscriptionPlanName,
                    SubscriptionPlanDisplayName = subscriptionData.SubscriptionPlanDisplayName,
                    SubscriptionStatus = 1, // Active
                    CurrentPeriodStart = subscriptionData.CurrentPeriodStart,
                    CurrentPeriodEnd = subscriptionData.CurrentPeriodEnd,
                    ActivatedAt = subscriptionData.ActivatedAt,
                    Action = "Activated"
                };

                await _publishEndpoint.Publish(cacheEvent);

                _logger.LogInformation(
                    "Published subscription cache event for UserId={UserId}, SubscriptionId={SubscriptionId}",
                    message.UpdatedBy, message.SubscriptionId);
            }
            else
            {
                _logger.LogWarning(
                    "No subscription data in result for SubscriptionId={SubscriptionId}",
                    message.SubscriptionId);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - event publishing failure shouldn't affect activation
            _logger.LogWarning(ex,
                "Failed to publish events for SubscriptionId={SubscriptionId}",
                message.SubscriptionId);
        }

        _logger.LogInformation(
            "Successfully processed ActivateSubscription: SubscriptionId={SubscriptionId}",
            message.SubscriptionId);
    }
}

