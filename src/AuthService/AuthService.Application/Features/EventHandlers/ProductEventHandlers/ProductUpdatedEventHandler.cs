using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using AuthService.Domain.Entities;
using System.Text.Json;


namespace ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers;

public class ProductUpdatedEventHandler : IIntegrationEventHandler<ProductUpdatedEvent>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<ProductUpdatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProductUpdatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling ProductUpdatedEvent: {ProductId}", @event.ProductId);

            // Check if user exists - skip if UpdatedBy is Guid.Empty or user doesn't exist
            if (@event.UpdatedBy == Guid.Empty)
            {
                _logger.LogWarning("ProductUpdatedEvent for {ProductId} has empty UpdatedBy. Skipping UserAction creation.", @event.ProductId);
                return;
            }

            // Verify user exists in the system before creating UserAction
            var userExists = await _unitOfWork.Repository<AppUser>()
                .GetFirstOrDefaultAsync(u => u.Id == @event.UpdatedBy);

            if (userExists == null)
            {
                _logger.LogWarning("User {UserId} not found for ProductUpdatedEvent {ProductId}. Skipping UserAction creation.", 
                    @event.UpdatedBy, @event.ProductId);
                return;
            }

            // Build change summary
            var changesSummary = @event.Changes.Any() 
                ? string.Join(", ", @event.Changes.Select(kv => $"{kv.Key}: {kv.Value.OldValue} → {kv.Value.NewValue}"))
                : "No specific changes tracked";

            // Create User Action log
            var userAction = new UserAction
            {
                UserId = @event.UpdatedBy,
                Action = UserActionEnum.Update,
                EntityId = @event.ProductId,
                EntityName = "Product",
                OldValue = @event.Changes.Any() ? JsonSerializer.Serialize(@event.Changes.ToDictionary(kv => kv.Key, kv => kv.Value.OldValue)) : null,
                NewValue = @event.Changes.Any() ? JsonSerializer.Serialize(@event.Changes.ToDictionary(kv => kv.Key, kv => kv.Value.NewValue)) : null,
                ActionDetail = $"Updated product '{@event.ProductName}': {changesSummary}",
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.ProductId);

            // Add to repository
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UserAction logged for ProductUpdated: {UserActionId}", userAction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ProductUpdatedEvent: {ProductId}", @event.ProductId);
            throw; // Re-throw to ensure proper error handling in EventBus
        }
    }
}
