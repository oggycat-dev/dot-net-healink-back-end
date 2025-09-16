using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Repositories;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;
using ProductAuthMicroservice.ProductService.Application.Features.Products.QueryBuilders;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginationResult<ProductItem>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductsQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetProductsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetProductsQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<PaginationResult<ProductItem>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user first (for public endpoints, this can be optional)
            var isValid = (await _currentUserService.IsUserValidAsync()).isValid;
            if (!isValid)
            {
                // For public product listing, we might allow anonymous access
                // return PaginationResult<ProductItem>.Failure("User is not valid", ErrorCodeEnum.Unauthorized);
                _logger.LogInformation("Anonymous user accessing products");
            }

            // Build query using extension methods
            var predicate = request.Filter.BuildPredicate();
            var orderBy = request.Filter.BuildOrderBy();
            var isAscending = request.Filter.IsAscending ?? false; // Default: giảm dần (newest first)
            var include = ProductQueryBuilder.BuildInclude(includeCategory: true);

            // Get paged data from repository
            var (items, totalCount) = await _unitOfWork.Repository<Product>().GetPagedAsync(
                pageNumber: request.Filter.Page,
                pageSize: request.Filter.PageSize,
                predicate: predicate,
                orderBy: orderBy,
                isAscending: isAscending
            );

            // Map to DTOs with category information
            var productItems = _mapper.Map<List<ProductItem>>(items);

            return PaginationResult<ProductItem>.Success(
                productItems,
                request.Filter.Page,
                request.Filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products with filter: {@Filter}", request.Filter);
            return PaginationResult<ProductItem>.Failure(
                "Error getting products",
                ErrorCodeEnum.InternalError);
        }
    }
}
