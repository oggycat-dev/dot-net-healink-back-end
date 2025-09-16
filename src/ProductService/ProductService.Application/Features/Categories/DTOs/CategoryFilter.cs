using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;

public class CategoryFilter : BasePaginationFilter
{
    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; set; }

    [JsonPropertyName("include_sub_categories")]
    public bool IncludeSubCategories { get; set; } = true;

    [JsonPropertyName("include_products_count")]
    public bool IncludeProductsCount { get; set; } = true;

    [JsonPropertyName("has_products")]
    public bool? HasProducts { get; set; }

    [JsonPropertyName("root_categories_only")]
    public bool RootCategoriesOnly { get; set; } = false;
}
