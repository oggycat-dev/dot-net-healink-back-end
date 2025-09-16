using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

public class CategoryRequestDto
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; set; }

    [JsonPropertyName("image_path")]
    [StringLength(255, ErrorMessage = "Image path cannot exceed 255 characters")]
    public string? ImagePath { get; set; }
}
