using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

public class ProductCategoryDto : BaseResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; set; }

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; set; }
}
