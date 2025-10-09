using NotificationService.Application;
using NotificationService.Infrastructure;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Auth;
using SharedLibrary.Contracts.User.Events;
using NotificationService.Infrastructure.EventHandlers;
using SharedLibrary.Commons.Extensions;

namespace NotificationService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for NotificationService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging + RabbitMQ EventBus)
        builder.ConfigureMicroserviceServices("NotificationService");

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Add MassTransit with consumers for notification workflow
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
        {
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendOtpNotificationConsumer>();
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendWelcomeNotificationConsumer>();
            // CreatorApplicationApproved uses RabbitMQ EventBus (not MassTransit)
        });

        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for NotificationService
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("NotificationService");

        return app;
    }
    
    /// <summary>
    /// Configure event subscriptions for NotificationService
    /// </summary>
    public static WebApplication ConfigureEventSubscriptions(this WebApplication app)
    {
        // Add RabbitMQ Event Bus
        app.AddRabbitMQEventBus();
        
        // Subscribe to auth events
        var eventBus = app.Services.GetRequiredService<IEventBus>();
        eventBus.Subscribe<ResetPasswordEvent, SendOtpResetPasswordEventHandler>();
        eventBus.Subscribe<CreatorApplicationApprovedEvent, CreatorApplicationApprovedEventHandler>();

        app.Services.SubscribeToAuthEvents();
        
        return app;
    }
}