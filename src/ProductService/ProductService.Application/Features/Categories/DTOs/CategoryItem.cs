using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

public class CategoryItem : BaseResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; set; }

    [JsonPropertyName("parent_category_name")]
    public string? ParentCategoryName { get; set; }

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("products_count")]
    public int ProductsCount { get; set; }

    [JsonPropertyName("sub_categories_count")]
    public int SubCategoriesCount { get; set; }

    [JsonPropertyName("has_products")]
    public bool HasProducts => ProductsCount > 0;

    [JsonPropertyName("has_sub_categories")]
    public bool HasSubCategories => SubCategoriesCount > 0;

    [JsonPropertyName("is_root_category")]
    public bool IsRootCategory => !ParentCategoryId.HasValue;
}
