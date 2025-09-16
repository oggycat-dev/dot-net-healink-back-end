using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQuery : IRequest<Result<CategoryResponseDto>>
{
    public Guid Id { get; set; }
    public bool IncludeSubCategories { get; set; } = true;
    public bool IncludeProductsCount { get; set; } = true;

    public GetCategoryByIdQuery(Guid id, bool includeSubCategories = true, bool includeProductsCount = true)
    {
        Id = id;
        IncludeSubCategories = includeSubCategories;
        IncludeProductsCount = includeProductsCount;
    }
}
