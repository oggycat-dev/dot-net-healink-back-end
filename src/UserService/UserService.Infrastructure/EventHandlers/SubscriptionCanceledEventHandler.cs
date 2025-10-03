using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionCanceledEvent
/// Infrastructure Layer: Subscribe event từ RabbitMQ và delegate tới CQRS Command
/// </summary>
public class SubscriptionCanceledEventHandler : IIntegrationEventHandler<SubscriptionCanceledEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionCanceledEventHandler> _logger;

    public SubscriptionCanceledEventHandler(
        IMediator mediator,
        ILogger<SubscriptionCanceledEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionCanceledEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionCanceledEvent: SubscriptionId={SubscriptionId}, UserProfileId={UserProfileId}, CanceledBy={CanceledBy}",
                @event.SubscriptionId,
                @event.UserProfileId,
                @event.CanceledBy);

            // Nếu không có CanceledBy thì skip (system action)
            if (@event.CanceledBy == null || @event.CanceledBy == Guid.Empty)
            {
                _logger.LogInformation("Skipping activity log - system action (no user)");
                return;
            }

            // Chuẩn bị metadata
            var metadata = new
            {
                EventId = @event.Id,
                EventType = nameof(SubscriptionCanceledEvent),
                SubscriptionId = @event.SubscriptionId,
                UserProfileId = @event.UserProfileId,
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.PlanName,
                CancelAtPeriodEnd = @event.CancelAtPeriodEnd,
                CancelAt = @event.CancelAt,
                CanceledAt = @event.CanceledAt,
                Reason = @event.Reason
            };

            // Chuẩn bị description với reason nếu có
            var description = @event.CancelAtPeriodEnd
                ? $"Scheduled cancellation of subscription '{@event.PlanName}' at period end ({@event.CancelAt:yyyy-MM-dd})"
                : $"Immediately canceled subscription '{@event.PlanName}'";

            if (!string.IsNullOrEmpty(@event.Reason))
            {
                description += $" - Reason: {@event.Reason}";
            }

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.CanceledBy.Value,
                ActivityType = "SubscriptionCanceled",
                Description = description,
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionCanceled event");
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
                "Error handling SubscriptionCanceledEvent: SubscriptionId={SubscriptionId}",
                @event.SubscriptionId);
        }
    }
}
