namespace ProductAuthMicroservice.Commons.Configs;

/// <summary>
/// Configuration for Outbox Event Processing
/// </summary>
public class OutboxConfig
{
    public const string SectionName = "OutboxProcessor";

    /// <summary>
    /// Processing interval in seconds
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Number of events to process in each batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum retry attempts for failed events
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable/disable outbox processing
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Processing timeout in seconds
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 300;
}
