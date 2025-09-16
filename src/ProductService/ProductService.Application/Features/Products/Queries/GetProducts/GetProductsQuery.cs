using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(ProductFilter Filter) : IRequest<PaginationResult<ProductItem>>;
