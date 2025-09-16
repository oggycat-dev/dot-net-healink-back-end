using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Services;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryResponseDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<CategoryResponseDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating category: {CategoryId}", request.Id);

            // 1. Get existing category
            var category = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Id == request.Id);

            if (category == null)
            {
                return Result<CategoryResponseDto>.Failure(
                    "Category not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 2. Validate Parent Category exists if specified and not same as current category
            Category? parentCategory = null;
            if (request.ParentCategoryId.HasValue)
            {
                if (request.ParentCategoryId.Value == request.Id)
                {
                    return Result<CategoryResponseDto>.Failure(
                        "Category cannot be its own parent", 
                        ErrorCodeEnum.ValidationFailed);
                }

                parentCategory = await _unitOfWork.Repository<Category>()
                    .GetFirstOrDefaultAsync(x => x.Id == request.ParentCategoryId.Value);

                if (parentCategory == null)
                {
                    return Result<CategoryResponseDto>.Failure(
                        "Parent category not found", 
                        ErrorCodeEnum.NotFound);
                }

                // Check for circular reference
                if (await IsCircularReference(request.Id, request.ParentCategoryId.Value))
                {
                    return Result<CategoryResponseDto>.Failure(
                        "Cannot set parent category as it would create a circular reference", 
                        ErrorCodeEnum.ValidationFailed);
                }
            }

            // 3. Check for duplicate name
            var existingCategory = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Name.ToLower() == request.Name.ToLower() && 
                                           x.ParentCategoryId == request.ParentCategoryId &&
                                           x.Id != request.Id);

            if (existingCategory != null)
            {
                return Result<CategoryResponseDto>.Failure(
                    "Category with this name already exists in the same parent category", 
                    ErrorCodeEnum.DuplicateEntry);
            }

            // 4. Capture old values for change tracking
            var oldValues = new
            {
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ImagePath = category.ImagePath
            };

            // 5. Update category properties
            category.Name = request.Name;
            category.Description = request.Description;
            category.ParentCategoryId = request.ParentCategoryId;
            category.ImagePath = request.ImagePath;
            category.UpdateEntity(Guid.Parse(_currentUserService.UserId??Guid.Empty.ToString()));

            // 6. Create change tracking
            var changes = new Dictionary<string, ChangeInfo>();

            if (oldValues.Name != category.Name)
                changes.Add("Name", new ChangeInfo { OldValue = oldValues.Name, NewValue = category.Name });

            if (oldValues.Description != category.Description)
                changes.Add("Description", new ChangeInfo { OldValue = oldValues.Description, NewValue = category.Description });

            if (oldValues.ParentCategoryId != category.ParentCategoryId)
                changes.Add("ParentCategoryId", new ChangeInfo { OldValue = oldValues.ParentCategoryId, NewValue = category.ParentCategoryId });

            if (oldValues.ImagePath != category.ImagePath)
                changes.Add("ImagePath", new ChangeInfo { OldValue = oldValues.ImagePath, NewValue = category.ImagePath });

            // 7. Update repository
            _unitOfWork.Repository<Category>().Update(category);

            // 8. Create Integration Event
            var categoryUpdatedEvent = new CategoryUpdatedEvent()
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                UpdatedBy = category.UpdatedBy ?? Guid.Empty,
                UpdatedAt = category.UpdatedAt ?? DateTime.UtcNow,
                Changes = changes
            };

            // 9. Add Outbox Event
            await _unitOfWork.AddOutboxEventAsync(categoryUpdatedEvent);

            // 10. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            // 11. Map and return response
            var response = _mapper.Map<CategoryResponseDto>(category);
            if (parentCategory != null)
            {
                response.ParentCategory = _mapper.Map<CategoryResponseDto>(parentCategory);
            }

            _logger.LogInformation("Category updated successfully: {CategoryId}", category.Id);

            return Result<CategoryResponseDto>.Success(response, "Category updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", request.Id);
            return Result<CategoryResponseDto>.Failure(
                "Failed to update category", 
                ErrorCodeEnum.InternalError);
        }
    }

    private async Task<bool> IsCircularReference(Guid categoryId, Guid targetParentId)
    {
        var currentParentId = targetParentId;

        while (currentParentId != Guid.Empty)
        {
            if (currentParentId == categoryId)
                return true;

            var parentCategory = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Id == currentParentId);

            if (parentCategory?.ParentCategoryId == null)
                break;

            currentParentId = parentCategory.ParentCategoryId.Value;
        }

        return false;
    }
}
