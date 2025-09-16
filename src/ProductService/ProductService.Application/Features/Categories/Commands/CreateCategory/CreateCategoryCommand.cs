using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommand : IRequest<Result<CategoryResponseDto>>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? ImagePath { get; set; }

    public CreateCategoryCommand(CategoryRequestDto request)
    {
        Name = request.Name;
        Description = request.Description;
        ParentCategoryId = request.ParentCategoryId;
        ImagePath = request.ImagePath;
    }
}
