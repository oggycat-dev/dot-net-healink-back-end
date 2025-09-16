using MediatR;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommand : IRequest<Result<CategoryResponseDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? ImagePath { get; set; }

    public UpdateCategoryCommand(Guid id, CategoryRequestDto request)
    {
        Id = id;
        Name = request.Name;
        Description = request.Description;
        ParentCategoryId = request.ParentCategoryId;
        ImagePath = request.ImagePath;
    }
}
