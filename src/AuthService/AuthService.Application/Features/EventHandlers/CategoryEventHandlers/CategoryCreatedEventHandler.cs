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
/// Handles CategoryCreatedEvent to log user actions in AuthService
/// </summary>
public class CategoryCreatedEventHandler : IIntegrationEventHandler<CategoryCreatedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryCreatedEventHandler> _logger;

    public CategoryCreatedEventHandler(
        IUnitOfWork unitOfWork,
        ILogger<CategoryCreatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CategoryCreatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing CategoryCreatedEvent for category: {CategoryId} - {CategoryName}", 
                @event.CategoryId, @event.CategoryName);

            // Create User Action log
            var userAction = new UserAction
            {
                UserId = @event.CreatedBy,
                Action = UserActionEnum.Create,
                EntityId = @event.CategoryId,
                EntityName = "Category",
                NewValue = $"Category '{@event.CategoryName}' created",
                ActionDetail = $"Created category: '{@event.CategoryName}'" +
                              (!string.IsNullOrEmpty(@event.Description) ? $" - {@event.Description}" : "") +
                              (@event.ParentCategoryId.HasValue ? $" under parent category: {@event.ParentCategoryName}" : " as root category") +
                              (!string.IsNullOrEmpty(@event.ImagePath) ? $" with image: {@event.ImagePath}" : ""),
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.CreatedBy);
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User action logged successfully for CategoryCreatedEvent: {CategoryId}", @event.CategoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CategoryCreatedEvent for category: {CategoryId}", @event.CategoryId);
            // Don't throw - we don't want to fail the entire transaction for logging issues
        }
    }
}
