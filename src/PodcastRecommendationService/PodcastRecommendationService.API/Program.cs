using PodcastRecommendationService.API.Configurations;
using SharedLibrary.Commons.Configurations;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("PodcastRecommendationService");

try
{
    logger.LogInformation("Starting PodcastRecommendationService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();
    
    logger.LogInformation("PodcastRecommendationService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "PodcastRecommendationService API failed to start");
    throw;
}
finally
{
    Serilog.Log.CloseAndFlush();
}