// Create startup logger
using NotificationService.API.Configurations;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Extensions;

var logger = LoggingConfiguration.CreateStartupLogger("NotificationService");

try
{
    logger.LogInformation("Starting NotificationService API...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services
    builder.ConfigureServices();

    var app = builder.Build();

    // Configure the application pipeline
    app.ConfigurePipeline();
    
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


