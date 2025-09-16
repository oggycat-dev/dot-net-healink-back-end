using System.Text.Json.Serialization;

namespace ProductAuthMicroservice.Commons.EventBus;

/// <summary>
/// Base class for integration events
/// </summary>
public abstract record IntegrationEvent
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("creation_date")]
    public DateTime CreationDate { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("source_service")]
    public string SourceService { get; init; } = string.Empty;

    protected IntegrationEvent()
    {
        EventType = GetType().Name;
    }

    protected IntegrationEvent(string sourceService) : this()
    {
        SourceService = sourceService;
    }
}