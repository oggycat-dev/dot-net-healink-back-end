using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

public class ProductRequestDto
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [JsonPropertyName("price")]
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [JsonPropertyName("discount_price")]
    [Range(0, double.MaxValue, ErrorMessage = "Discount price must be greater than or equal to 0")]
    public decimal? DiscountPrice { get; set; }

    [JsonPropertyName("stock_quantity")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be greater than or equal to 0")]
    public int StockQuantity { get; set; } = 0;

    [JsonPropertyName("category_id")]
    [Required(ErrorMessage = "Category ID is required")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("is_pre_order")]
    public bool IsPreOrder { get; set; } = false;

    [JsonPropertyName("pre_order_release_date")]
    public DateTime? PreOrderReleaseDate { get; set; }
}
