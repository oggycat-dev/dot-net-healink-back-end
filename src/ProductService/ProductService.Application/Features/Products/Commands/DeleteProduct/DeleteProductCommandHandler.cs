using MediatR;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Outbox;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting product: {ProductId}", request.Id);

            // 1. Get existing product
            var product = await _unitOfWork.Repository<Product>()
                .GetFirstOrDefaultAsync(x => x.Id == request.Id);

            if (product == null)
            {
                return Result.Failure("Product not found", ErrorCodeEnum.NotFound);
            }

            // 2. Soft delete (mark as inactive)
            product.Status = EntityStatusEnum.Inactive;
            product.UpdateEntity();
            _unitOfWork.Repository<Product>().Update(product);

            // 3. Create Integration Event
            var productDeletedEvent = new ProductDeletedEvent()
            {
                ProductId = product.Id,
                ProductName = product.Name,
                DeletedBy = product.UpdatedBy ?? Guid.Empty
            };

            // 4. Add Outbox Event
            await _unitOfWork.AddOutboxEventAsync(productDeletedEvent);

            // 5. Save with Outbox Transaction
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Product deleted successfully: {ProductId}", product.Id);

            return Result.Success("Product deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product: {ProductId}", request.Id);
            return Result.Failure("Failed to delete product", ErrorCodeEnum.InternalError);
        }
    }
}
