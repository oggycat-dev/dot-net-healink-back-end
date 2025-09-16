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

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductResponseDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateProductCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProductResponseDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating product: {ProductId}", request.Id);

            // 1. Get existing product
            var product = await _unitOfWork.Repository<Product>()
                .GetFirstOrDefaultAsync(x => x.Id == request.Id);

            if (product == null)
            {
                return Result<ProductResponseDto>.Failure(
                    "Product not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 2. Validate Category exists
            var category = await _unitOfWork.Repository<Category>()
                .GetFirstOrDefaultAsync(x => x.Id == request.Request.CategoryId);

            if (category == null)
            {
                return Result<ProductResponseDto>.Failure(
                    "Category not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 3. Capture old values for event
            var oldValues = new
            {
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId
            };

            // 4. Update Product properties
            product.Name = request.Request.Name;
            product.Description = request.Request.Description;
            product.Price = request.Request.Price;
            product.DiscountPrice = request.Request.DiscountPrice;
            product.StockQuantity = request.Request.StockQuantity;
            product.CategoryId = request.Request.CategoryId;
            product.IsPreOrder = request.Request.IsPreOrder;
            product.PreOrderReleaseDate = request.Request.PreOrderReleaseDate;

            // Get current user ID for audit tracking
            var currentUserId = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : (Guid?)null;
            product.UpdateEntity(currentUserId);

            // 5. Update repository
            _unitOfWork.Repository<Product>().Update(product);

            // 6. Create Integration Event with change tracking
            var changes = new Dictionary<string, ChangeInfo>();
            
            if (oldValues.Name != product.Name)
                changes.Add("Name", new ChangeInfo { OldValue = oldValues.Name, NewValue = product.Name });
            
            if (oldValues.Price != product.Price)
                changes.Add("Price", new ChangeInfo { OldValue = oldValues.Price, NewValue = product.Price });
            
            if (oldValues.StockQuantity != product.StockQuantity)
                changes.Add("StockQuantity", new ChangeInfo { OldValue = oldValues.StockQuantity, NewValue = product.StockQuantity });
            
            if (oldValues.CategoryId != product.CategoryId)
                changes.Add("CategoryId", new ChangeInfo { OldValue = oldValues.CategoryId, NewValue = product.CategoryId });

            var productUpdatedEvent = new ProductUpdatedEvent()
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UpdatedBy = product.UpdatedBy ?? Guid.Empty,
                Changes = changes
            };

            // 7. Add Outbox Event
            await _unitOfWork.AddOutboxEventAsync(productUpdatedEvent);

            // 8. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            // 9. Map and return response
            var response = _mapper.Map<ProductResponseDto>(product);
            response.Category = _mapper.Map<ProductCategoryDto>(category);

            _logger.LogInformation("Product updated successfully: {ProductId}", product.Id);

            return Result<ProductResponseDto>.Success(response, "Product updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product: {ProductId}", request.Id);
            return Result<ProductResponseDto>.Failure(
                "Failed to update product", 
                ErrorCodeEnum.InternalError);
        }
    }
}
