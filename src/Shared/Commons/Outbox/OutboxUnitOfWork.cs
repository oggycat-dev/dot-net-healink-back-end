using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Repositories;

namespace ProductAuthMicroservice.Commons.Outbox;

public class OutboxUnitOfWork : UnitOfWork, IOutboxUnitOfWork
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxUnitOfWork> _logger;
    private readonly List<OutboxEvent> _pendingEvents = new();

    public OutboxUnitOfWork(DbContext context, IEventBus eventBus, ILogger<OutboxUnitOfWork> logger) : base(context)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task AddOutboxEventAsync<T>(T @event) where T : IntegrationEvent
    {
        try
        {
            //1. Init new outbox event
            var outboxEvent = new OutboxEvent
            {
                EventType = @event.GetType().AssemblyQualifiedName!, // Use full assembly qualified name
                AggregateId = @event.Id,
                EventData = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Status = Enums.EntityStatusEnum.Active
            };

            outboxEvent.InitializeEntity();
            _pendingEvents.Add(outboxEvent);
            await Repository<OutboxEvent>().AddAsync(outboxEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding outbox event");
            throw;
        }
    }

    public async Task<List<OutboxEvent>> GetPendingOutboxEventsAsync(int batchSize = 100)
    {
        try
        {
            var batchResult = await Repository<OutboxEvent>()
            .GetQueryable()
            .Where(e => e.ProcessedAt == null &&
                       (e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
            return batchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending outbox events");
            throw;
        }
    }

    public async Task MarkEventAsFailedAsync(Guid eventId, string errorMessage)
    {
        try
        {
            var outboxEvent = await Repository<OutboxEvent>().GetFirstOrDefaultAsync(x => x.Id == eventId);
            if (outboxEvent != null)
            {
                outboxEvent.RetryCount++;
                outboxEvent.ErrorMessage = errorMessage;
                outboxEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, outboxEvent.RetryCount));
                Repository<OutboxEvent>().Update(outboxEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking event as failed");
            throw;
        }
    }

    public async Task MarkEventAsProcessedAsync(Guid eventId)
    {
        try
        {
            var outboxEvent = await Repository<OutboxEvent>().GetFirstOrDefaultAsync(x => x.Id == eventId);
            if (outboxEvent != null)
            {
                outboxEvent.ProcessedAt = DateTime.UtcNow;
                outboxEvent.UpdateEntity();
                Repository<OutboxEvent>().Update(outboxEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking event as processed");
            throw;
        }
    }

    public async Task SaveChangesWithOutboxAsync(CancellationToken cancellationToken = default)
    {
        // Use execution strategy to handle retrying execution strategy
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Start a new transaction inside the execution strategy
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Save domain changes and outbox events
                await SaveChangesAsync(cancellationToken);

                // Publish events after successful save
                await PublishPendingEventsAsync();

                // Commit the transaction
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save changes with outbox events");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task PublishPendingEventsAsync()
    {
        foreach (var outboxEvent in _pendingEvents)
        {
            try
            {
                _logger.LogInformation("Publishing outbox event {EventId} of type {EventType}", outboxEvent.Id, outboxEvent.EventType);
                
                var eventType = Type.GetType(outboxEvent.EventType);
                if (eventType != null)
                {
                    var @event = JsonSerializer.Deserialize(outboxEvent.EventData, eventType, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (@event is IntegrationEvent integrationEvent)
                    {
                        _logger.LogInformation("Publishing event {EventId} to RabbitMQ", integrationEvent.Id);
                        await _eventBus.PublishAsync(integrationEvent);
                        await MarkEventAsProcessedAsync(outboxEvent.Id);
                        _logger.LogInformation("Successfully published and marked event {EventId} as processed", outboxEvent.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Deserialized event {EventId} is not an IntegrationEvent", outboxEvent.Id);
                    }
                }
                else
                {
                    _logger.LogError("Could not resolve event type {EventType} for event {EventId}", outboxEvent.EventType, outboxEvent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId} of type {EventType}", outboxEvent.Id, outboxEvent.EventType);
                await MarkEventAsFailedAsync(outboxEvent.Id, ex.Message);
            }
        }

        _pendingEvents.Clear();
    }
}