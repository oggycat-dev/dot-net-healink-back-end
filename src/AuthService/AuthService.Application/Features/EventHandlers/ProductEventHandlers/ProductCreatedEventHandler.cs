using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using AuthService.Domain.Entities;

namespace ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers;

public class ProductCreatedEventHandler : IIntegrationEventHandler<ProductCreatedEvent>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<ProductCreatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling ProductCreatedEvent: {ProductId}", @event.ProductId);

            // Create User Action log
            var userAction = new UserAction
            {
                UserId = @event.CreatedBy,
                Action = UserActionEnum.Create,
                EntityId = @event.ProductId,
                EntityName = "Product",
                NewValue = $"Product '{@event.ProductName}' created with price {@event.Price:C}",
                ActionDetail = $"Created product in category {@event.CategoryId} with stock quantity {@event.StockQuantity}",
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.CreatedBy);

            // Add to repository
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UserAction logged for ProductCreated: {UserActionId}", userAction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ProductCreatedEvent: {ProductId}", @event.ProductId);
            throw; // Re-throw to ensure proper error handling in EventBus
        }
    }
}
