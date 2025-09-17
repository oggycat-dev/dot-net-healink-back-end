using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Repositories;

namespace SharedLibrary.Commons.Outbox;

public interface IOutboxUnitOfWork : IUnitOfWork
{
    Task AddOutboxEventAsync<T>(T @event) where T : IntegrationEvent;
    Task<List<OutboxEvent>> GetPendingOutboxEventsAsync(int batchSize = 100);
    Task MarkEventAsProcessedAsync(Guid eventId);
    Task MarkEventAsFailedAsync(Guid eventId, string errorMessage);
    Task SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default);
}