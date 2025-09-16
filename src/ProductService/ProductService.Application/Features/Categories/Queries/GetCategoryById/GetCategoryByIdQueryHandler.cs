using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Repositories;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

    public GetCategoryByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCategoryByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CategoryResponseDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting category by ID: {CategoryId}", request.Id);

            // Get category by ID with includes
            var category = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(
                    c => c.Id == request.Id && !c.IsDeleted,
                    c => c.ParentCategory
                );

            // Load subcategories separately if needed
            if (category != null && request.IncludeSubCategories)
            {
                await _unitOfWork.Repository<Category>().FindAsync(
                    sc => sc.ParentCategoryId == category.Id && !sc.IsDeleted
                );
            }

            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", request.Id);
                return Result<CategoryResponseDto>.Failure(
                    "Category not found", 
                    ErrorCodeEnum.NotFound);
            }

            // Map to DTO
            var categoryDto = _mapper.Map<CategoryResponseDto>(category);

            // Calculate products count if requested
            if (request.IncludeProductsCount)
            {
                var productsCount = await _unitOfWork.Repository<Product>()
                    .CountAsync(p => p.CategoryId == category.Id && !p.IsDeleted);
                
                categoryDto.ProductsCount = productsCount;

                // Also calculate products count for subcategories if included
                if (request.IncludeSubCategories && categoryDto.SubCategories.Any())
                {
                    foreach (var subCategory in categoryDto.SubCategories)
                    {
                        if (Guid.TryParse(subCategory.Id, out var subCategoryId))
                        {
                            var subProductsCount = await _unitOfWork.Repository<Product>()
                                .CountAsync(p => p.CategoryId == subCategoryId && !p.IsDeleted);
                            
                            subCategory.ProductsCount = subProductsCount;
                        }
                    }
                }
            }

            _logger.LogInformation("Category retrieved successfully: {CategoryId} - {CategoryName}", 
                category.Id, category.Name);

            return Result<CategoryResponseDto>.Success(categoryDto, "Category retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category: {CategoryId}", request.Id);
            return Result<CategoryResponseDto>.Failure(
                "Failed to retrieve category", 
                ErrorCodeEnum.InternalError);
        }
    }
}
