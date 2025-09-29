using ContentService.API.Configurations;
using ContentService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Extensions;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("ContentService");

try
{
    logger.LogInformation("Starting ContentService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();
    
    // Apply database migrations
    await app.ApplyContentMigrationsAsync(logger);
    
    // Configure RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to events
    app.Services.SubscribeToAuthEvents();
    
    logger.LogInformation("ContentService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "ContentService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("ContentService API shutting down");
}
