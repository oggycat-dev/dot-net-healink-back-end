using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionPlanDeletedEvent
/// Infrastructure Layer: Subscribe event từ RabbitMQ và delegate tới CQRS Command
/// </summary>
public class SubscriptionPlanDeletedEventHandler : IIntegrationEventHandler<SubscriptionPlanDeletedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionPlanDeletedEventHandler> _logger;

    public SubscriptionPlanDeletedEventHandler(
        IMediator mediator,
        ILogger<SubscriptionPlanDeletedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionPlanDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionPlanDeletedEvent: PlanId={PlanId}, Name={Name}, DeletedBy={DeletedBy}",
                @event.SubscriptionPlanId,
                @event.Name,
                @event.DeletedBy);

            // Nếu không có DeletedBy thì skip (system action)
            if (@event.DeletedBy == null || @event.DeletedBy == Guid.Empty)
            {
                _logger.LogInformation("Skipping activity log - system action (no user)");
                return;
            }

            // Chuẩn bị metadata
            var metadata = new
            {
                EventId = @event.Id,
                EventType = nameof(SubscriptionPlanDeletedEvent),
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.Name,
                DisplayName = @event.DisplayName,
                DeletedAt = @event.DeletedAt
            };

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.DeletedBy.Value,
                ActivityType = "SubscriptionPlanDeleted",
                Description = $"Deleted subscription plan '{@event.DisplayName}' ({@event.Name})",
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionPlanDeleted event");
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
                "Error handling SubscriptionPlanDeletedEvent: PlanId={PlanId}",
                @event.SubscriptionPlanId);
        }
    }
}
