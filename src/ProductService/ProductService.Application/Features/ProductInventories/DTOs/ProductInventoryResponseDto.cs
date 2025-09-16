using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.DTOs;

public class ProductInventoryResponseDto : BaseResponse
{
    [JsonPropertyName("product_id")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;
}
