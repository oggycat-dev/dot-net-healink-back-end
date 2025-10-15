using System.Text.Json.Serialization;

namespace SharedLibrary.Commons.EventBus;

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

    public Guid? CreatedBy { get; init; }
    public Guid? UpdatedBy { get; init; }
    public Guid? DeletedBy { get; init; }
    /// <summary>
    /// IP Address of the user who triggered the event (null for system/background events)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; init; }

    /// <summary>
    /// User Agent of the request that triggered the event (null for system/background events)
    /// </summary>
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; init; }

    protected IntegrationEvent()
    {
        EventType = GetType().Name;
    }

    protected IntegrationEvent(string sourceService) : this()
    {
        SourceService = sourceService;
    }
}