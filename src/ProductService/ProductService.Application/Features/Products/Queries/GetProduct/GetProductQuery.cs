using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<Result<ProductResponseDto>>;
