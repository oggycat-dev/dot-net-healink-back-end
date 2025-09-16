using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;

public class ProductFilter : BasePaginationFilter
{
    [JsonPropertyName("category_id")]
    public Guid? CategoryId { get; set; }

    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; set; }

    [JsonPropertyName("is_pre_order")]
    public bool? IsPreOrder { get; set; }

    [JsonPropertyName("in_stock")]
    public bool? InStock { get; set; }

    [JsonPropertyName("min_stock")]
    public int? MinStock { get; set; }

    [JsonPropertyName("max_stock")]
    public int? MaxStock { get; set; }
}
