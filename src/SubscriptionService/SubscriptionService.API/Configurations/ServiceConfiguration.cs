using SubscriptionService.Application;
using SubscriptionService.Infrastructure;
using SubscriptionService.Infrastructure.Context;
using SubscriptionService.Infrastructure.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Contracts.Payment.Requests;
using MassTransit;

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

        // CRITICAL: Add MassTransit with Saga and Entity Framework Outbox
        // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
        builder.Services.AddMassTransitWithSaga<SubscriptionDbContext>(
            builder.Configuration,
            configureSagas: x =>
            {
                // Configure RegisterSubscription Saga
                SubscriptionSagaConfiguration.ConfigureRegisterSubscriptionSaga<SubscriptionDbContext>(x);
            },
            configureConsumers: x =>
            {
                // Register consumers for SubscriptionService
                x.AddConsumer<Infrastructure.Consumers.ActivateSubscriptionConsumer>();
                x.AddConsumer<Infrastructure.Consumers.CancelSubscriptionConsumer>();
                x.AddConsumer<Infrastructure.Consumers.GetUserSubscriptionConsumer>();
            },
            configureEndpoints: (cfg, context) =>
            {
                // Configure saga-specific endpoints with Entity Framework Outbox
                SubscriptionSagaConfiguration.ConfigureSagaEndpoints(cfg, context);
            });

        // Application & Infrastructure layers
        builder.Services.AddSubscriptionApplication();
        builder.Services.AddSubscriptionInfrastructure(builder.Configuration);

        // ✅ Register Request Client for payment intent creation (RPC)
        // This allows RegisterSubscriptionCommandHandler to call PaymentService synchronously
        builder.Services.AddScoped(provider =>
        {
            var bus = provider.GetRequiredService<MassTransit.IBus>();
            return bus.CreateRequestClient<CreatePaymentIntentRequest>(RequestTimeout.After(s: 30));
        });

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
