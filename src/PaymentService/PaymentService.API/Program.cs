using PaymentService.API.Configurations;
using PaymentService.Infrastructure.Extensions;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Extensions;

// Create startup logger
var logger = LoggingConfiguration.CreateStartupLogger("PaymentService");

try
{
    logger.LogInformation("Starting PaymentService API...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure all services
    builder.ConfigureServices();
    
    var app = builder.Build();
    
    // Configure pipeline
    app.ConfigurePipeline();
    
    // Apply database migrations
    await app.ApplyPaymentMigrationsAsync(logger);
    
    // Seed payment data
    await app.SeedPaymentDataAsync(logger);
    
    // Configure RabbitMQ Event Bus
    app.AddRabbitMQEventBus();
    
    // Subscribe to events
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    eventBus.StartConsuming();
    
    logger.LogInformation("PaymentService API configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "PaymentService API failed to start");
    throw;
}
finally
{
    logger.LogInformation("PaymentService API shutting down");
}
