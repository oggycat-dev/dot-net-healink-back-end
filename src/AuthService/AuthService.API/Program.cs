using AuthService.API.Configurations;
using AuthService.Infrastructure.Extensions;
using ProductAuthMicroservice.Commons.Configurations;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Services;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("AuthService");

try
{
    logger.LogInformation("Starting AuthService API...");
    
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services
    builder.ConfigureServices();
    
    // Add distributed authentication
    builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);
    
    // Add current user service for auth
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    var app = builder.Build();

    // Configure middleware pipeline
    app.ConfigurePipeline();

    // Apply database migrations for AuthService (MUST be before seeding)
    await app.ApplyAuthMigrationsAsync(logger);

    // Seed authentication data (Identity-specific for AuthService)
    await app.SeedAuthDataAsync(logger);

    // Add RabbitMQ Event Bus and subscribe to events
    app.AddRabbitMQEventBus();
    
    // Subscribe to Product Events
    var eventBus = app.Services.GetRequiredService<ProductAuthMicroservice.Commons.EventBus.IEventBus>();
    eventBus.Subscribe<ProductCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductCreatedEventHandler>();
    eventBus.Subscribe<ProductUpdatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductUpdatedEventHandler>();
    eventBus.Subscribe<ProductInventoryCreatedEvent, ProductAuthMicroservice.AuthService.Application.Features.EventHandlers.ProductEventHandlers.ProductInventoryCreatedEventHandler>();
    
    // Subscribe to auth events for distributed authentication
    app.Services.SubscribeToAuthEvents();
    
    // Start consuming events
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