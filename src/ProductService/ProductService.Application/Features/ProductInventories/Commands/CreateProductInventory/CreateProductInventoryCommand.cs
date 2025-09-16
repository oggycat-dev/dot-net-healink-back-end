using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.Commands.CreateProductInventory;

public record CreateProductInventoryCommand : IRequest<Result<ProductInventoryResponseDto>>
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public string? Note { get; init; }
}
