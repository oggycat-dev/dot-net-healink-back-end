using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

public class ProductItem : BaseResponse
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

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("is_pre_order")]
    public bool IsPreOrder { get; set; }

    [JsonPropertyName("pre_order_release_date")]
    public DateTime? PreOrderReleaseDate { get; set; }

    [JsonPropertyName("final_price")]
    public decimal FinalPrice => DiscountPrice ?? Price;

    [JsonPropertyName("is_on_sale")]
    public bool IsOnSale => DiscountPrice.HasValue && DiscountPrice < Price;

    [JsonPropertyName("discount_percentage")]
    public decimal? DiscountPercentage => IsOnSale 
        ? Math.Round((Price - DiscountPrice!.Value) / Price * 100, 2) 
        : null;
}
