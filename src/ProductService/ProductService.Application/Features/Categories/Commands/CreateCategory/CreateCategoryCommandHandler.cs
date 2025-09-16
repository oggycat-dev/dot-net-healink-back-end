using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryResponseDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<CategoryResponseDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating category: {CategoryName}", request.Name);

            // 1. Validate Parent Category exists if specified
            Category? parentCategory = null;
            if (request.ParentCategoryId.HasValue)
            {
                parentCategory = await _unitOfWork.Repository<Category>()
                    .GetFirstOrDefaultAsync(x => x.Id == request.ParentCategoryId.Value);

                if (parentCategory == null)
                {
                    return Result<CategoryResponseDto>.Failure(
                        "Parent category not found", 
                        ErrorCodeEnum.NotFound);
                }
            }

            // 2. Check for duplicate category name
            var existingCategory = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Name.ToLower() == request.Name.ToLower() && 
                                           x.ParentCategoryId == request.ParentCategoryId);

            if (existingCategory != null)
            {
                return Result<CategoryResponseDto>.Failure(
                    "Category with this name already exists in the same parent category", 
                    ErrorCodeEnum.DuplicateEntry);
            }

            // 3. Create Category entity
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                ParentCategoryId = request.ParentCategoryId,
                ImagePath = request.ImagePath,
                Status = EntityStatusEnum.Active
            };

            category.InitializeEntity(Guid.Parse(_currentUserService.UserId??Guid.Empty.ToString()));

            // 4. Add to repository
            await _unitOfWork.Repository<Category>().AddAsync(category);

            // 5. Create Integration Event
            var categoryCreatedEvent = new CategoryCreatedEvent()
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = parentCategory?.Name,
                ImagePath = category.ImagePath,
                CreatedBy = category.CreatedBy ?? Guid.Empty,
                CreatedAt = category.CreatedAt ?? DateTime.UtcNow
            };

            // 6. Add Outbox Event (Same Transaction)
            await _unitOfWork.AddOutboxEventAsync(categoryCreatedEvent);

            // 7. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            // 8. Map and return response
            var response = _mapper.Map<CategoryResponseDto>(category);
            if (parentCategory != null)
            {
                response.ParentCategory = _mapper.Map<CategoryResponseDto>(parentCategory);
            }

            _logger.LogInformation("Category created successfully: {CategoryId}", category.Id);

            return Result<CategoryResponseDto>.Success(response, "Category created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category: {CategoryName}", request.Name);
            return Result<CategoryResponseDto>.Failure(
                "Failed to create category", 
                ErrorCodeEnum.InternalError);
        }
    }
}
