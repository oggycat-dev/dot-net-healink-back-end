using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Subscription.Events;
using UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;
using System.Text.Json;

namespace UserService.Infrastructure.EventHandlers;

/// <summary>
/// Event Handler để xử lý SubscriptionRegisteredActivityEvent
/// Infrastructure Layer: Subscribe event từ Custom Outbox (legacy RabbitMQ EventBus) và delegate tới CQRS Command
/// </summary>
public class SubscriptionRegisteredActivityEventHandler : IIntegrationEventHandler<SubscriptionRegisteredActivityEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionRegisteredActivityEventHandler> _logger;

    public SubscriptionRegisteredActivityEventHandler(
        IMediator mediator,
        ILogger<SubscriptionRegisteredActivityEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SubscriptionRegisteredActivityEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Received SubscriptionRegisteredActivityEvent: SubscriptionId={SubscriptionId}, UserProfileId={UserProfileId}, CreatedBy={CreatedBy}",
                @event.SubscriptionId,
                @event.UserProfileId,
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
                EventType = nameof(SubscriptionRegisteredActivityEvent),
                CorrelationId = @event.CorrelationId,
                SubscriptionId = @event.SubscriptionId,
                UserProfileId = @event.UserProfileId,
                SubscriptionPlanId = @event.SubscriptionPlanId,
                PlanName = @event.SubscriptionPlanName,
                DisplayName = @event.SubscriptionPlanDisplayName,
                Amount = @event.Amount,
                Currency = @event.Currency,
                CreatedAt = @event.CreatedAt
            };

            // Gọi Command để ghi log (CQRS pattern)
            var command = new CreateUserActivityLogCommand
            {
                UserId = @event.CreatedBy.Value,
                ActivityType = @event.ActivityType, // "SubscriptionRegistered"
                Description = string.IsNullOrEmpty(@event.Description) 
                    ? $"Registered subscription for plan '{@event.SubscriptionPlanDisplayName}' ({@event.Amount} {@event.Currency})"
                    : @event.Description,
                Metadata = JsonSerializer.Serialize(metadata),
                // Extract from event (captured at source - RegisterSubscriptionCommandHandler)
                IpAddress = @event.IpAddress,
                UserAgent = @event.UserAgent
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User activity log created successfully for SubscriptionRegistered event. SubscriptionId={SubscriptionId}",
                    @event.SubscriptionId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create user activity log for SubscriptionId={SubscriptionId}: {ErrorMessage}",
                    @event.SubscriptionId,
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling SubscriptionRegisteredActivityEvent: SubscriptionId={SubscriptionId}",
                @event.SubscriptionId);
        }
    }
}

