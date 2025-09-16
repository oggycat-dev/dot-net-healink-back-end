namespace ProductAuthMicroservice.Commons.EventBus;

public interface IEventBus
{
    /// <summary>
    /// Publish an event asynchronously
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="event">Event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IntegrationEvent;

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <typeparam name="TH">Event handler type</typeparam>
    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    /// <summary>
    /// Unsubscribe from an event type
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <typeparam name="TH">Event handler type</typeparam>
    void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>;

    /// <summary>
    /// Start consuming messages
    /// </summary>
    void StartConsuming();

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    void StopConsuming();
}