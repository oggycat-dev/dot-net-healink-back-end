using PaymentService.Application;
using PaymentService.Infrastructure;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;

namespace PaymentService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for PaymentService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("PaymentService");

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Add MassTransit without Saga (PaymentService doesn't manage saga state)
        builder.Services.AddMassTransitWithConsumers(
            builder.Configuration, x =>
            {
                // Register consumers for PaymentService
                // TODO: Add payment consumers when needed
            });

        // Application & Infrastructure layers
        builder.Services.AddPaymentApplication();
        builder.Services.AddPaymentInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for PaymentService
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("PaymentService");

        return app;
    }
}