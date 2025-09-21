// Create startup logger
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Extensions;
using UserService.API.Configurations;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Infrastructure.Extensions;

var logger = LoggingConfiguration.CreateStartupLogger("UserService");

try
{
    logger.LogInformation("Starting UserService API...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services
    builder.ConfigureServices();
    
    // Add distributed authentication
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);


    var app = builder.Build();

    // Configure middleware pipeline
    app.ConfigurePipeline();

    // Apply database migrations for UserService
    await app.ApplyUserMigrationsAsync(logger);

    // Seed user data (business roles and admin profile)
    await app.SeedUserDataAsync(logger);

    // Add RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to auth events for distributed authentication
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