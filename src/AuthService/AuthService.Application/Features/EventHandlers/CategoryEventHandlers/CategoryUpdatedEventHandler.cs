using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Repositories;
using AuthService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;
using System.Text.Json;
using ProductAuthMicroservice.Commons.EventBus;

namespace ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.CategoryEventHandlers;

/// <summary>
/// Handles CategoryUpdatedEvent to log user actions in AuthService
/// </summary>
public class CategoryUpdatedEventHandler : IIntegrationEventHandler<CategoryUpdatedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryUpdatedEventHandler> _logger;

    public CategoryUpdatedEventHandler(
        IUnitOfWork unitOfWork,
        ILogger<CategoryUpdatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CategoryUpdatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing CategoryUpdatedEvent for category: {CategoryId} - {CategoryName}", 
                @event.CategoryId, @event.CategoryName);

            // Build change summary
            var changesSummary = @event.Changes.Any() 
                ? string.Join(", ", @event.Changes.Select(kv => $"{kv.Key}: {kv.Value.OldValue} → {kv.Value.NewValue}"))
                : "No specific changes tracked";

            // Create User Action log
            var userAction = new UserAction
            {
                UserId = @event.UpdatedBy,
                Action = UserActionEnum.Update,
                EntityId = @event.CategoryId,
                EntityName = "Category",
                OldValue = @event.Changes.Any() 
                    ? JsonSerializer.Serialize(@event.Changes.ToDictionary(kv => kv.Key, kv => kv.Value.OldValue)) 
                    : null,
                NewValue = @event.Changes.Any() 
                    ? JsonSerializer.Serialize(@event.Changes.ToDictionary(kv => kv.Key, kv => kv.Value.NewValue)) 
                    : null,
                ActionDetail = $"Updated category '{@event.CategoryName}': {changesSummary}",
                Status = EntityStatusEnum.Active
            };

            userAction.InitializeEntity(@event.UpdatedBy);
            await _unitOfWork.Repository<UserAction>().AddAsync(userAction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User action logged successfully for CategoryUpdatedEvent: {CategoryId}", @event.CategoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CategoryUpdatedEvent for category: {CategoryId}", @event.CategoryId);
            // Don't throw - we don't want to fail the entire transaction for logging issues
        }
    }
}
