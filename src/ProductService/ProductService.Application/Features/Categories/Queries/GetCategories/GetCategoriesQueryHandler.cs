using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Repositories;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.QueryBuilders;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, PaginationResult<CategoryItem>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCategoriesQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<PaginationResult<CategoryItem>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user first (for public endpoints, this can be optional)
            var isValid = (await _currentUserService.IsUserValidAsync()).isValid;
            if (!isValid)
            {
                // For public category listing, we might allow anonymous access
                _logger.LogInformation("Anonymous user accessing categories");
            }

            // Build query using extension methods
            var predicate = request.Filter.BuildPredicate();
            var orderBy = request.Filter.BuildOrderBy();
            var isAscending = request.Filter.IsAscending ?? true; // Default: tăng dần (alphabetical)
            var include = CategoryQueryBuilder.BuildInclude(
                includeParent: true,
                includeSubCategories: request.Filter.IncludeSubCategories,
                includeProducts: false
            );

            // Get paged data from repository
            var (items, totalCount) = await _unitOfWork.Repository<Category>().GetPagedAsync(
                pageNumber: request.Filter.Page,
                pageSize: request.Filter.PageSize,
                predicate: predicate,
                orderBy: orderBy,
                isAscending: isAscending
            );

            // Map to DTOs
            var categoryItems = _mapper.Map<List<CategoryItem>>(items);

            // Calculate products count if requested
            if (request.Filter.IncludeProductsCount)
            {
                await CalculateProductsCount(categoryItems);
            }

            return PaginationResult<CategoryItem>.Success(
                categoryItems,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories with filter: {@Filter}", request.Filter);
            return PaginationResult<CategoryItem>.Failure(
                "Error getting categories",
                ErrorCodeEnum.InternalError);
        }
    }

    private async Task CalculateProductsCount(List<CategoryItem> categories)
    {
        foreach (var category in categories)
        {
            if (Guid.TryParse(category.Id, out var categoryId))
            {
                var productsCount = await _unitOfWork.Repository<Product>()
                    .CountAsync(p => p.CategoryId == categoryId && !p.IsDeleted);
                
                var subCategoriesCount = await _unitOfWork.Repository<Category>()
                    .CountAsync(c => c.ParentCategoryId == categoryId && !c.IsDeleted);
                
                category.ProductsCount = productsCount;
                category.SubCategoriesCount = subCategoriesCount;
            }
        }
    }
}
