using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.Commons.Models;

public abstract class BaseResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("createdBy")]
    public Guid? CreatedBy { get; set; }
    [JsonPropertyName("updatedBy")]
    public Guid? UpdatedBy { get; set; }
    [JsonPropertyName("status")]
    public EntityStatusEnum Status { get; set; }

}