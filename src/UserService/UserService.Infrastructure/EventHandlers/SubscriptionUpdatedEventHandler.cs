using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionUpdatedEvent
/// Infrastructure Layer: Subscribe event từ RabbitMQ và delegate tới CQRS Command
/// </summary>
public class SubscriptionUpdatedEventHandler : IIntegrationEventHandler<SubscriptionUpdatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionUpdatedEventHandler> _logger;

    public SubscriptionUpdatedEventHandler(
        IMediator mediator,
        ILogger<SubscriptionUpdatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionUpdatedEvent: SubscriptionId={SubscriptionId}, UserProfileId={UserProfileId}, UpdatedBy={UpdatedBy}",
                @event.SubscriptionId,
                @event.UserProfileId,
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
                EventType = nameof(SubscriptionUpdatedEvent),
                SubscriptionId = @event.SubscriptionId,
                UserProfileId = @event.UserProfileId,
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.PlanName,
                SubscriptionStatus = @event.SubscriptionStatus,
                RenewalBehavior = @event.RenewalBehavior,
                CancelAtPeriodEnd = @event.CancelAtPeriodEnd,
                CurrentPeriodEnd = @event.CurrentPeriodEnd
            };

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.UpdatedBy.Value,
                ActivityType = "SubscriptionUpdated",
                Description = $"Updated subscription for plan '{@event.PlanName}' - Status: {@event.SubscriptionStatus}, Renewal: {@event.RenewalBehavior}",
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionUpdated event");
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
                "Error handling SubscriptionUpdatedEvent: SubscriptionId={SubscriptionId}",
                @event.SubscriptionId);
        }
    }
}
