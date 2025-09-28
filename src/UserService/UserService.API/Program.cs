using UserService.API.Configurations;
using UserService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.DependencyInjection;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("UserService");

try
{
    logger.LogInformation("Starting UserService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();

    // Apply database migrations
    await app.ApplyUserMigrationsAsync(logger);

    // Seed user data
    await app.SeedUserDataAsync(logger);

    // Add RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to auth events
    app.Services.SubscribeToAuthEvents();

    logger.LogInformation("UserService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "UserService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("UserService API shutting down");
}