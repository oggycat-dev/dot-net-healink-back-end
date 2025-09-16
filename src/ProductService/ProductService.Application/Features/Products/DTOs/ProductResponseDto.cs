using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

public class ProductResponseDto : BaseResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("discount_price")]
    public decimal? DiscountPrice { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("is_pre_order")]
    public bool IsPreOrder { get; set; }

    [JsonPropertyName("pre_order_release_date")]
    public DateTime? PreOrderReleaseDate { get; set; }

    [JsonPropertyName("category")]
    public ProductCategoryDto? Category { get; set; }

    [JsonPropertyName("images")]
    public List<ProductImageResponseDto> Images { get; set; } = new();
}
