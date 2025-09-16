using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(Guid Id, ProductRequestDto Request) : IRequest<Result<ProductResponseDto>>;
