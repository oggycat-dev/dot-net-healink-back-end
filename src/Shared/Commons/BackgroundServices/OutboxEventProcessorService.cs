using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Outbox;
using System.Text.Json;

namespace ProductAuthMicroservice.Commons.BackgroundServices;

public class OutboxEventProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventProcessorService> _logger;
    private readonly OutboxConfig _config;

    public OutboxEventProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventProcessorService> logger,
        IOptions<OutboxConfig> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Event Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            // Wait before next processing cycle
            await Task.Delay(TimeSpan.FromSeconds(_config.ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox Event Processor Service stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOutboxUnitOfWork>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        try
        {
            // Get pending events
            var pendingEvents = await unitOfWork.GetPendingOutboxEventsAsync(_config.BatchSize);

            if (!pendingEvents.Any())
            {
                _logger.LogDebug("No pending outbox events found");
                return;
            }

            _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count);

            foreach (var outboxEvent in pendingEvents)
            {
                await ProcessSingleEventAsync(outboxEvent, eventBus, unitOfWork, cancellationToken);
            }

            _logger.LogInformation("Completed processing {Count} outbox events", pendingEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessPendingEventsAsync");
            throw;
        }
    }

    private async Task ProcessSingleEventAsync(
        OutboxEvent outboxEvent, 
        IEventBus eventBus, 
        IOutboxUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing outbox event: {EventId} of type {EventType}", 
                outboxEvent.Id, outboxEvent.EventType);

            // Attempt to resolve event type
            var eventType = ResolveEventType(outboxEvent.EventType);
            if (eventType == null)
            {
                _logger.LogWarning("Could not resolve event type: {EventType}", outboxEvent.EventType);
                await unitOfWork.MarkEventAsFailedAsync(outboxEvent.Id, $"Unknown event type: {outboxEvent.EventType}");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Deserialize event
            var @event = JsonSerializer.Deserialize(outboxEvent.EventData, eventType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (@event is not IntegrationEvent integrationEvent)
            {
                _logger.LogWarning("Event is not an IntegrationEvent: {EventType}", outboxEvent.EventType);
                await unitOfWork.MarkEventAsFailedAsync(outboxEvent.Id, $"Invalid event format: {outboxEvent.EventType}");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // Publish event
            await eventBus.PublishAsync(integrationEvent, cancellationToken);

            // Mark as processed
            await unitOfWork.MarkEventAsProcessedAsync(outboxEvent.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully processed outbox event: {EventId}", outboxEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox event: {EventId}", outboxEvent.Id);

            // Mark as failed with retry logic
            await unitOfWork.MarkEventAsFailedAsync(outboxEvent.Id, ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private Type? ResolveEventType(string eventTypeName)
    {
        try
        {
            // Try to find the type in loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == eventTypeName && 
                                   typeof(IntegrationEvent).IsAssignableFrom(t));
                
                if (type != null)
                {
                    return type;
                }
            }

            _logger.LogWarning("Could not resolve event type: {EventTypeName}", eventTypeName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving event type: {EventTypeName}", eventTypeName);
            return null;
        }
    }
}
