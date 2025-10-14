using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Subscription.Commands;
using SubscriptionService.Application.Features.Subscriptions.HandleSubscriptionSagaCommand;

namespace SubscriptionService.Infrastructure.Consumers;

/// <summary>
/// Consumer for CancelSubscription command from saga (compensation/rollback)
/// Delegates to CQRS command handler (Clean Architecture pattern)
/// </summary>
public class CancelSubscriptionConsumer : IConsumer<CancelSubscription>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CancelSubscriptionConsumer> _logger;

    public CancelSubscriptionConsumer(
        IMediator mediator,
        ILogger<CancelSubscriptionConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelSubscription> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received CancelSubscription command: SubscriptionId={SubscriptionId}, IsCompensation={IsCompensation}, Reason={Reason}",
            message.SubscriptionId, message.IsCompensation, message.Reason);

        // ✅ Delegate to CQRS command handler
        var command = new HandleSubscriptionSagaCommand
        {
            SubscriptionId = message.SubscriptionId,
            Action = SubscriptionSagaAction.Cancel,
            Reason = message.Reason,
            IsCompensation = message.IsCompensation,
            UpdatedBy = message.UpdatedBy
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Failed to cancel subscription: SubscriptionId={SubscriptionId}, Error={Error}",
                message.SubscriptionId, result.Message);
            throw new Exception($"Failed to cancel subscription: {result.Message}");
        }

        _logger.LogInformation(
            "Successfully processed CancelSubscription: SubscriptionId={SubscriptionId}",
            message.SubscriptionId);
    }
}

