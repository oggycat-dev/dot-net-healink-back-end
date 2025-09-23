using NotificationService.Application;
using NotificationService.Infrastructure;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;

namespace NotificationService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for NotificationService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services
        builder.ConfigureMicroserviceServices("NotificationService");

        // Add MassTransit with consumers for notification workflow
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
        {
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendOtpNotificationConsumer>();
            x.AddConsumer<NotificationService.Infrastructure.Consumers.SendWelcomeNotificationConsumer>();
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
}