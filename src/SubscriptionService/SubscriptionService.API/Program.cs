using SubscriptionService.API.Configurations;
using SubscriptionService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Extensions;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("SubscriptionService");

try
{
    logger.LogInformation("Starting SubscriptionService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();
    
    // Apply database migrations
    await app.ApplySubscriptionMigrationsAsync(logger);
    
    // Seed subscription data
    await app.SeedSubscriptionDataAsync(logger);
    
    // Configure RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to events
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    eventBus.StartConsuming();
    
    logger.LogInformation("SubscriptionService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "SubscriptionService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("SubscriptionService API shutting down");
}
