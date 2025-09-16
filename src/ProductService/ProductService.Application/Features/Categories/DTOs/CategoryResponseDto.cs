using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

public class CategoryResponseDto : BaseResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; set; }

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("parent_category")]
    public CategoryResponseDto? ParentCategory { get; set; }

    [JsonPropertyName("sub_categories")]
    public ICollection<CategoryResponseDto> SubCategories { get; set; } = new List<CategoryResponseDto>();

    [JsonPropertyName("products_count")]
    public int ProductsCount { get; set; }
}
