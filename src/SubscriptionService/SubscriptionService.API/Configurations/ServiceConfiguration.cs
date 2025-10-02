using SubscriptionService.Application;
using SubscriptionService.Infrastructure;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;

namespace SubscriptionService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for SubscriptionService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("SubscriptionService");

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Add MassTransit with Saga for subscription workflow
        builder.Services.AddMassTransitWithSaga<SubscriptionService.Infrastructure.Context.SubscriptionDbContext>(
            builder.Configuration, x =>
            {
                // Register consumers for SubscriptionService
                
            });

        // Application & Infrastructure layers
        builder.Services.AddSubscriptionApplication();
        builder.Services.AddSubscriptionInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for SubscriptionService
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("SubscriptionService");

        return app;
    }
}