using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Repositories;
using AuthService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.EventBus;

namespace ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.CategoryEventHandlers;

/// <summary>
/// Handles CategoryDeletedEvent to log user actions in AuthService
/// </summary>
public class CategoryDeletedEventHandler : IIntegrationEventHandler<CategoryDeletedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryDeletedEventHandler> _logger;

    public CategoryDeletedEventHandler(
        IUnitOfWork unitOfWork,
        ILogger<CategoryDeletedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CategoryDeletedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing CategoryDeletedEvent for category: {CategoryId} - {CategoryName}", 
                @event.CategoryId, @event.CategoryName);

            // Create User Action log
            var userAction = new UserAction
            {
                UserId = @event.DeletedBy,
                Action = UserActionEnum.Delete,
                EntityId = @event.CategoryId,
                EntityName = "Category",
                OldValue = $"Category '{@event.CategoryName}' deleted",
                ActionDetail = $"Deleted category: '{@event.CategoryName}'" +
                              (@event.AffectedProductsCount > 0 ? $" (had {@event.AffectedProductsCount} products)" : "") +
                              (@event.SubCategoriesCount > 0 ? $" (had {@event.SubCategoriesCount} subcategories)" : ""),
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.DeletedBy);
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User action logged successfully for CategoryDeletedEvent: {CategoryId}", @event.CategoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CategoryDeletedEvent for category: {CategoryId}", @event.CategoryId);
            // Don't throw - we don't want to fail the entire transaction for logging issues
        }
    }
}
