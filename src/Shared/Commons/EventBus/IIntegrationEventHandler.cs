namespace ProductAuthMicroservice.Commons.EventBus;


/// <summary>
/// Interface for handling integration events
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for event handlers
/// </summary>
public interface IIntegrationEventHandler
{
}