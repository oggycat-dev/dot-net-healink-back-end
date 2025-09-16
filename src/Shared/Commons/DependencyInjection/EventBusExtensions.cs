using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.BackgroundServices;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.EventBus;

namespace ProductAuthMicroservice.Commons.DependencyInjection;

/// <summary>
/// Extension methods for configuring RabbitMQ Event Bus
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Add RabbitMQ Event Bus services to DI container
    /// </summary>
    public static IServiceCollection AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure RabbitMQ
        services.Configure<RabbitMQConfig>(configuration.GetSection(RabbitMQConfig.SectionName));
        
        // Configure Outbox Processor
        services.Configure<OutboxConfig>(configuration.GetSection(OutboxConfig.SectionName));
        
        // Register Event Bus components
        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        
        // Register Background Service for Outbox processing
        services.AddHostedService<OutboxEventProcessorService>();
        
        return services;
    }

    /// <summary>
    /// Configure RabbitMQ Event Bus in application pipeline
    /// </summary>
    public static WebApplication AddRabbitMQEventBus(this WebApplication app)
    {
        // Initialize Event Bus connection
        var eventBus = app.Services.GetRequiredService<IEventBus>();
        
        // Event Bus will automatically connect when first used
        app.Logger.LogInformation("RabbitMQ Event Bus configured successfully");
        
        return app;
    }
}
