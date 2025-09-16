using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(ProductRequestDto Request) : IRequest<Result<ProductResponseDto>>;
