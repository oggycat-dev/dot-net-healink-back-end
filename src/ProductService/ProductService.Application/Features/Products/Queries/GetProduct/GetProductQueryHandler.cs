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

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProduct;

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, Result<ProductResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetProductQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetProductQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProductResponseDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user first (for public endpoints, this can be optional)
            var isValid = (await _currentUserService.IsUserValidAsync()).isValid;
            if (!isValid)
            {
                // For public product access, we might allow anonymous access
                _logger.LogInformation("Anonymous user accessing product: {ProductId}", request.Id);
            }

            // Get product by ID with includes
            var product = await _unitOfWork.Repository<Product>()
                .GetFirstOrDefaultAsync(
                    p => p.Id == request.Id && !p.IsDeleted,
                    p => p.Category,
                    p => p.ProductImages
                );

            if (product == null)
            {
                return Result<ProductResponseDto>.Failure("Product not found", ErrorCodeEnum.NotFound);
            }

            // Map to DTO
            var productResponse = _mapper.Map<ProductResponseDto>(product);

            return Result<ProductResponseDto>.Success(productResponse, "Product retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product: {ProductId}", request.Id);
            return Result<ProductResponseDto>.Failure("Error getting product", ErrorCodeEnum.InternalError);
        }
    }
}
