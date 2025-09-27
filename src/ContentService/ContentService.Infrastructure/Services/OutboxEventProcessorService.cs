using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Outbox;

namespace ContentService.Infrastructure.Services;

public class OutboxEventProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventProcessorService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30); // Process every 30 seconds

    public OutboxEventProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Event Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEvents(stoppingToken);
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait longer on error
            }
        }

        _logger.LogInformation("Outbox Event Processor Service stopped");
    }

    private async Task ProcessOutboxEvents(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxUnitOfWork = scope.ServiceProvider.GetRequiredService<IOutboxUnitOfWork>();

        try
        {
            var pendingEvents = await outboxUnitOfWork.GetPendingOutboxEventsAsync(batchSize: 50);
            
            if (pendingEvents.Count == 0)
            {
                return; // No events to process
            }

            _logger.LogInformation("Processing {Count} outbox events", pendingEvents.Count);

            foreach (var outboxEvent in pendingEvents)
            {
                try
                {
                    // Here you would normally publish to your message bus (RabbitMQ, Service Bus, etc.)
                    // For now, we'll just mark as processed
                    // 
                    // Example:
                    // await _eventBus.PublishAsync(outboxEvent);
                    
                    await outboxUnitOfWork.MarkEventAsProcessedAsync(outboxEvent.Id);
                    
                    _logger.LogDebug("Outbox event {EventId} of type {EventType} processed successfully", 
                        outboxEvent.Id, outboxEvent.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}", 
                        outboxEvent.Id, outboxEvent.EventType);
                    
                    await outboxUnitOfWork.MarkEventAsFailedAsync(outboxEvent.Id, ex.Message);
                }
            }

            await outboxUnitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Completed processing {Count} outbox events", pendingEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during outbox event processing batch");
        }
    }
}