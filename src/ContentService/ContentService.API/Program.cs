using ContentService.Application;
using ContentService.Infrastructure;
using ContentService.API.Configurations;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.EventBus;
using Serilog;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("ContentService");

try
{
    logger.LogInformation("Starting ContentService API...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services using the configuration pattern
    builder.ConfigureServices();
    
    // Add distributed authentication
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

    var app = builder.Build();

    // Configure middleware pipeline
    app.ConfigurePipeline();

    // Add RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to auth events for distributed authentication
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
