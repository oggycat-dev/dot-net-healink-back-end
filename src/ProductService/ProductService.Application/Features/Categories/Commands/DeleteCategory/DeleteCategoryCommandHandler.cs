using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    public DeleteCategoryCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting category: {CategoryId}", request.Id);

            // 1. Get existing category
            var category = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, c => c.SubCategories, c => c.Products);

            if (category == null)
            {
                return Result.Failure(
                    "Category not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 2. Check if category has subcategories
            var subCategoriesCount = category.SubCategories?.Count ?? 0;
            if (subCategoriesCount > 0)
            {
                return Result.Failure(
                    $"Cannot delete category that has {subCategoriesCount} subcategories. Please delete or move subcategories first.", 
                    ErrorCodeEnum.ValidationFailed);
            }

            // 3. Check if category has products
            var affectedProductsCount = category.Products?.Count ?? 0;
            if (affectedProductsCount > 0)
            {
                return Result.Failure(
                    $"Cannot delete category that has {affectedProductsCount} products. Please delete or move products first.", 
                    ErrorCodeEnum.ValidationFailed);
            }

            // 4. Soft delete the category
            category.SoftDeleteEnitity(Guid.Parse(_currentUserService.UserId??Guid.Empty.ToString()));

            // 5. Update repository
            _unitOfWork.Repository<Category>().Update(category);

            // 6. Create Integration Event
            var categoryDeletedEvent = new CategoryDeletedEvent()
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                DeletedBy = category.DeletedBy ?? Guid.Empty,
                DeletedAt = category.DeletedAt ?? DateTime.UtcNow,
                AffectedProductsCount = affectedProductsCount,
                SubCategoriesCount = subCategoriesCount
            };

            // 7. Add Outbox Event
            await _unitOfWork.AddOutboxEventAsync(categoryDeletedEvent);

            // 8. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Category deleted successfully: {CategoryId}", category.Id);

            return Result.Success("Category deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", request.Id);
            return Result.Failure(
                "Failed to delete category", 
                ErrorCodeEnum.InternalError);
        }
    }
}
