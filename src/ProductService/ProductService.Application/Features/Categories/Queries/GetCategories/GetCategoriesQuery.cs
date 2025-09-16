using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery(CategoryFilter Filter) : IRequest<PaginationResult<CategoryItem>>;
