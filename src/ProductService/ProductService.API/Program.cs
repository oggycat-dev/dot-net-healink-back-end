using ProductService.API.Configurations;
using ProductAuthMicroservice.Commons.Configurations;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Commons.BackgroundServices;
using ProductService.Application;
using ProductService.Infrastructure;
using ProductService.Infrastructure.Extensions;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Services;
using ProductAuthMicroservice.Commons.Configs;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("ProductService");

try
{
    logger.LogInformation("Starting ProductService API...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services
    builder.ConfigureServices();
    
    // Add distributed authentication
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
    
    // Add application and infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    
    // Add current user service for auth
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    var app = builder.Build();

    // Configure middleware pipeline
    app.ConfigurePipeline();

    // Apply database migrations for ProductService
    await app.ApplyProductMigrationsAsync(logger);

    // Add RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to auth events for distributed authentication
    app.Services.SubscribeToAuthEvents();

    logger.LogInformation("ProductService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "ProductService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("ProductService API shutting down");
}