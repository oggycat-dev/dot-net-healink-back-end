using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionPlanCreatedEvent
/// Infrastructure Layer: Subscribe event từ RabbitMQ và delegate tới CQRS Command
/// </summary>
public class SubscriptionPlanCreatedEventHandler : IIntegrationEventHandler<SubscriptionPlanCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionPlanCreatedEventHandler> _logger;

    public SubscriptionPlanCreatedEventHandler(
        IMediator mediator,
        ILogger<SubscriptionPlanCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionPlanCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionPlanCreatedEvent: PlanId={PlanId}, Name={Name}, CreatedBy={CreatedBy}",
                @event.SubscriptionPlanId,
                @event.Name,
                @event.CreatedBy);

            // Nếu không có CreatedBy thì skip (system action)
            if (@event.CreatedBy == null || @event.CreatedBy == Guid.Empty)
            {
                _logger.LogInformation("Skipping activity log - system action (no user)");
                return;
            }

            // Chuẩn bị metadata
            var metadata = new
            {
                EventId = @event.Id,
                EventType = nameof(SubscriptionPlanCreatedEvent),
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.Name,
                DisplayName = @event.DisplayName,
                Amount = @event.Amount,
                Currency = @event.Currency,
                BillingPeriod = $"{@event.BillingPeriodCount} {@event.BillingPeriodUnit}",
                TrialDays = @event.TrialDays
            };

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.CreatedBy.Value,
                ActivityType = "SubscriptionPlanCreated",
                Description = $"Created subscription plan '{@event.DisplayName}' ({@event.Name})",
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionPlanCreated event");
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
                "Error handling SubscriptionPlanCreatedEvent: PlanId={PlanId}",
                @event.SubscriptionPlanId);
        }
    }
}
