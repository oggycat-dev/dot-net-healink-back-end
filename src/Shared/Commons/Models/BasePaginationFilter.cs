using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.Commons.Models;

public class BasePaginationFilter
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;
    [JsonPropertyName("search")]
    public string? Search { get; set; }
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; }
    [JsonPropertyName("isAscending")]
    public bool? IsAscending { get; set; }
    [JsonPropertyName("status")]
    public EntityStatusEnum? Status { get; set; }
}