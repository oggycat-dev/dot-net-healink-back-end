using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using AuthService.Domain.Entities;

namespace ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers;

public class ProductInventoryCreatedEventHandler : IIntegrationEventHandler<ProductInventoryCreatedEvent>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<ProductInventoryCreatedEventHandler> _logger;

    public ProductInventoryCreatedEventHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<ProductInventoryCreatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProductInventoryCreatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling ProductInventoryCreatedEvent: {InventoryId}", @event.InventoryId);

            // Create User Action log for stock-in operation
            var userAction = new UserAction
            {
                UserId = @event.CreatedBy,
                Action = UserActionEnum.Create,
                EntityId = @event.InventoryId,
                EntityName = "ProductInventory",
                NewValue = $"Stock-in: +{@event.Quantity} units",
                ActionDetail = $"Added {@event.Quantity} units to '{@event.ProductName}'. New total stock: {@event.NewTotalStock}",
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.CreatedBy);

            // Add to repository
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UserAction logged for ProductInventoryCreated: {UserActionId}", userAction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ProductInventoryCreatedEvent: {InventoryId}", @event.InventoryId);
            throw; // Re-throw to ensure proper error handling in EventBus
        }
    }
}
