namespace ProductAuthMicroservice.Commons.Configs;

/// <summary>
/// RabbitMQ configuration for v7.1.2
/// </summary>
public class RabbitMQConfig
{
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ server hostname
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ server port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host to connect to
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Exchange name for routing events
    /// </summary>
    public string ExchangeName { get; set; } = "product_auth_exchange";

    /// <summary>
    /// Queue name prefix (will be auto-generated if empty)
    /// </summary>
    public string QueueName { get; set; } = "";

    /// <summary>
    /// Whether exchanges and queues should survive server restarts
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Whether to delete exchanges/queues when no longer in use
    /// </summary>
    public bool AutoDelete { get; set; } = false;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for exponential backoff retry
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Whether to automatically acknowledge messages (should be false for reliability)
    /// </summary>
    public bool AutoAck { get; set; } = false;

    /// <summary>
    /// Number of unacknowledged messages that a consumer can handle
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// SSL server name (for SSL connections)
    /// </summary>
    public string SslServerName { get; set; } = "";
}