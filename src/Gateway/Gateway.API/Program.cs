using Gateway.API.Configuration;
using SharedLibrary.Commons.Configurations;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("Gateway");

try
{
    logger.LogInformation("Starting Gateway API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    await app.ConfigurePipelineAsync();
    
    logger.LogInformation("Gateway API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Gateway API failed to start");
    throw;
}
finally
{
    logger.LogInformation("Gateway API shutting down");
}