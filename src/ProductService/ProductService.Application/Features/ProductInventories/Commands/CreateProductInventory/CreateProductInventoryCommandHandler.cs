using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.Commands.CreateProductInventory;

public class CreateProductInventoryCommandHandler : IRequestHandler<CreateProductInventoryCommand, Result<ProductInventoryResponseDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductInventoryCommandHandler> _logger;

    public CreateProductInventoryCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateProductInventoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProductInventoryResponseDto>> Handle(CreateProductInventoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating product inventory for product: {ProductId}", request.ProductId);

            // 1. Validate Product exists
            var product = await _unitOfWork.Repository<Product>()
                .GetFirstOrDefaultAsync(x => x.Id == request.ProductId);

            if (product == null)
            {
                return Result<ProductInventoryResponseDto>.Failure(
                    "Product not found", 
                    ErrorCodeEnum.NotFound);
            }

            // 2. Create ProductInventory entity (Stock-in history)
            var inventory = new ProductInventory
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity, // This represents the quantity being added (stock-in)
                Status = EntityStatusEnum.Active
            };

            inventory.InitializeEntity(Guid.Parse(_currentUserService.UserId??Guid.Empty.ToString()));

            // 3. Update Product's total stock quantity
            product.StockQuantity += request.Quantity;
            product.UpdateEntity();

            // 4. Add to repositories
            await _unitOfWork.Repository<ProductInventory>().AddAsync(inventory);
            _unitOfWork.Repository<Product>().Update(product);

            // 5. Create Integration Event (Stock-in event)
            var inventoryCreatedEvent = new ProductInventoryCreatedEvent()
            {
                InventoryId = inventory.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = inventory.Quantity, // Quantity added
                NewTotalStock = product.StockQuantity, // New total after stock-in
                CreatedBy = inventory.CreatedBy ?? Guid.Empty
            };

            // 6. Add Outbox Event
            await _unitOfWork.AddOutboxEventAsync(inventoryCreatedEvent);

            // 7. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            // 8. Map and return response
            var response = _mapper.Map<ProductInventoryResponseDto>(inventory);
            response.ProductName = product.Name;

            _logger.LogInformation("Product inventory created successfully: {InventoryId}", inventory.Id);

            return Result<ProductInventoryResponseDto>.Success(response, "Product inventory created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product inventory for product: {ProductId}", request.ProductId);
            return Result<ProductInventoryResponseDto>.Failure(
                "Failed to create product inventory", 
                ErrorCodeEnum.InternalError);
        }
    }
}
