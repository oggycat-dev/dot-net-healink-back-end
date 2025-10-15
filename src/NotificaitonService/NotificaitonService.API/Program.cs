using NotificationService.API.Configurations;
using SharedLibrary.Commons.Configurations;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("NotificationService");

try
{
    logger.LogInformation("Starting NotificationService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();

    var app = builder.Build();

    // Configure pipeline
    app.ConfigurePipeline();
    
    // Configure event subscriptions
    app.ConfigureEventSubscriptions();
    
    logger.LogInformation("NotificationService API configured successfully");

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "NotificationService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("NotificationService API shutting down");
}


