using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionPlanUpdatedEvent
/// Infrastructure Layer: Subscribe event từ RabbitMQ và delegate tới CQRS Command
/// </summary>
public class SubscriptionPlanUpdatedEventHandler : IIntegrationEventHandler<SubscriptionPlanUpdatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionPlanUpdatedEventHandler> _logger;

    public SubscriptionPlanUpdatedEventHandler(
        IMediator mediator,
        ILogger<SubscriptionPlanUpdatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionPlanUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionPlanUpdatedEvent: PlanId={PlanId}, Name={Name}, UpdatedBy={UpdatedBy}",
                @event.SubscriptionPlanId,
                @event.Name,
                @event.UpdatedBy);

            // Nếu không có UpdatedBy thì skip (system action)
            if (@event.UpdatedBy == null || @event.UpdatedBy == Guid.Empty)
            {
                _logger.LogInformation("Skipping activity log - system action (no user)");
                return;
            }

            // Chuẩn bị metadata
            var metadata = new
            {
                EventId = @event.Id,
                EventType = nameof(SubscriptionPlanUpdatedEvent),
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.Name,
                DisplayName = @event.DisplayName,
                Amount = @event.Amount,
                BillingPeriodCount = @event.BillingPeriodCount,
                TrialDays = @event.TrialDays
            };

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.UpdatedBy.Value,
                ActivityType = "SubscriptionPlanUpdated",
                Description = $"Updated subscription plan '{@event.DisplayName}' ({@event.Name})",
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionPlanUpdated event");
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create user activity log: {ErrorMessage}",
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling SubscriptionPlanUpdatedEvent: PlanId={PlanId}",
                @event.SubscriptionPlanId);
        }
    }
}
