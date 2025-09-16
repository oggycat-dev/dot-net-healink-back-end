using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Attributes;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductResponseDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;

    public CreateProductCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateProductCommandHandler> logger,
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
    }

    public async Task<Result<ProductResponseDto>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate user từ distributed cache (CRITICAL CHECK)
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Result<ProductResponseDto>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized);
            }

            // 2. Check user state từ cache
            var userState = await _userStateCache.GetUserStateAsync(userId);
            if (userState == null)
            {
                _logger.LogWarning("User {UserId} not found in cache during product creation", userId);
                return Result<ProductResponseDto>.Failure("User session not found", ErrorCodeEnum.Unauthorized);
            }

            // 3. Check user is active
            if (!userState.IsActive)
            {
                _logger.LogWarning("Inactive user {UserId} attempted to create product", userId);
                return Result<ProductResponseDto>.Failure("User account is inactive", ErrorCodeEnum.Unauthorized);
            }

            // 4. Check roles từ cache (Admin hoặc Staff có thể tạo products)
            var hasRequiredRole = await _userStateCache.HasRoleAsync(userId, "Admin") || 
                                 await _userStateCache.HasRoleAsync(userId, "Staff");
            if (!hasRequiredRole)
            {
                _logger.LogWarning("User {UserId} with roles [{Roles}] attempted to create product without permission", 
                    userId, string.Join(", ", userState.Roles));
                return Result<ProductResponseDto>.Failure("Insufficient permissions to create product", ErrorCodeEnum.Forbidden);
            }

            _logger.LogInformation("User {UserId} creating product: {ProductName}", userId, command.Request.Name);

            // 5. Validate Category exists
            var category = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Id == command.Request.CategoryId && !x.IsDeleted);

            if (category == null)
            {
                return Result<ProductResponseDto>.Failure(
                    "Category not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 6. Create Product entity
            var product = _mapper.Map<Product>(command.Request);
            
            try
            {
                product.InitializeEntity(userId);
                await _unitOfWork.Repository<Product>().AddAsync(product);

                // 7. Create Integration Event
                var productCreatedEvent = new ProductCreatedEvent()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CategoryName = category.Name,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    CategoryId = product.CategoryId,
                    StockQuantity = product.StockQuantity,
                    CreatedBy = product.CreatedBy ?? Guid.Empty,
                    CreatedAt = product.CreatedAt ?? DateTime.UtcNow
                };

                // 8. Add Outbox Event (Same Transaction)
                await _unitOfWork.AddOutboxEventAsync(productCreatedEvent);

                // 9. Save changes with outbox (handles transaction internally)
                await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", command.Request.Name);
                throw;
            }

            // 10. Map and return response
            var response = _mapper.Map<ProductResponseDto>(product);
            response.Category = _mapper.Map<ProductCategoryDto>(category);

            _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

            return Result<ProductResponseDto>.Success(response, "Product created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product: {ProductName}", command.Request?.Name);
            return Result<ProductResponseDto>.Failure(
                "Failed to create product", 
                ErrorCodeEnum.InternalError);
        }
    }
}
