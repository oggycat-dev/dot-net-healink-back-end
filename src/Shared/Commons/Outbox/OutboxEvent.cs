using System.Text.Json.Serialization;
using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.Commons.Outbox;

public class OutboxEvent : BaseEntity
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("aggregate_id")]
    public Guid AggregateId { get; set; }

    [JsonPropertyName("event_data")]
    public string EventData { get; set; } = string.Empty;

    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [JsonPropertyName("retry_count")]
    public int RetryCount { get; set; } = 0;

    [JsonPropertyName("max_retry_count")]
    public int MaxRetryCount { get; set; } = 3;

    [JsonPropertyName("next_retry_at")]
    public DateTime? NextRetryAt { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    public bool IsProcessed => ProcessedAt.HasValue;
    public bool CanRetry => RetryCount < MaxRetryCount;
}