using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Subscription.Requests;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Infrastructure.Consumers;

/// <summary>
/// Consumer to handle GetUserSubscriptionRequest RPC calls
/// Returns user's active subscription data for cache loading
/// </summary>
public class GetUserSubscriptionConsumer : IConsumer<GetUserSubscriptionRequest>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserSubscriptionConsumer> _logger;

    public GetUserSubscriptionConsumer(
        IOutboxUnitOfWork unitOfWork,
        ILogger<GetUserSubscriptionConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetUserSubscriptionRequest> context)
    {
        var request = context.Message;

        _logger.LogInformation(
            "Processing GetUserSubscriptionRequest for UserProfileId={UserProfileId}",
            request.UserProfileId);

        try
        {
            // Find active subscription for the user
            var subscription = await _unitOfWork.Repository<Subscription>()
                .GetFirstOrDefaultAsync(
                    s => s.UserProfileId == request.UserProfileId && 
                         s.SubscriptionStatus == Domain.Enums.SubscriptionStatus.Active,
                    s => s.Plan);

            if (subscription == null)
            {
                _logger.LogInformation(
                    "No active subscription found for UserProfileId={UserProfileId}",
                    request.UserProfileId);

                await context.RespondAsync(new GetUserSubscriptionResponse
                {
                    Found = false
                });
                return;
            }

            _logger.LogInformation(
                "Found active subscription for UserProfileId={UserProfileId}: SubscriptionId={SubscriptionId}, Plan={PlanName}",
                request.UserProfileId, subscription.Id, subscription.Plan?.DisplayName);

            // Return subscription data
            await context.RespondAsync(new GetUserSubscriptionResponse
            {
                Found = true,
                SubscriptionId = subscription.Id,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                SubscriptionPlanName = subscription.Plan?.Name,
                SubscriptionPlanDisplayName = subscription.Plan?.DisplayName,
                SubscriptionStatus = (int)subscription.SubscriptionStatus, // Convert enum to int
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                CanceledAt = subscription.CanceledAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing GetUserSubscriptionRequest for UserProfileId={UserProfileId}",
                request.UserProfileId);

            await context.RespondAsync(new GetUserSubscriptionResponse
            {
                Found = false
            });
        }
    }
}
