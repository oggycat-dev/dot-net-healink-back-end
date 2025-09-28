using AuthService.API.Configurations;
using AuthService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Extensions;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("AuthService");

try
{
    logger.LogInformation("Starting AuthService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();
    
    // Apply database migrations
    await app.ApplyAuthMigrationsAsync(logger);
    
    // Seed auth data
    await app.SeedAuthDataAsync(logger);
    
    // Configure RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to events
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    app.Services.SubscribeToAuthEvents();
    eventBus.StartConsuming();
    
    logger.LogInformation("AuthService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "AuthService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("AuthService API shutting down");
}


