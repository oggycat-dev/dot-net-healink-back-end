using PaymentService.Application;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Context;
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

        // CRITICAL: Add MassTransit with Entity Framework Outbox
        // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
        // This enables transactional outbox for both HTTP handlers (Bus Outbox) and consumers
        builder.Services.AddMassTransitWithConsumers<PaymentDbContext>(
            builder.Configuration, 
            configureConsumers: x =>
            {
                // Register Request-Response consumer for payment intent creation
                x.AddConsumer<PaymentService.Infrastructure.Consumers.CreatePaymentIntentConsumer>();
            },
            useEntityFrameworkOutbox: true,  // Enable Entity Framework Outbox
            useBusOutbox: true);             // Enable Bus Outbox for IPublishEndpoint

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