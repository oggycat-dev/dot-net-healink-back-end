using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;
using AuthService.Infrastructure.Context;

namespace AuthService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for AuthService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("AuthService");

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Add OTP cache service for registration/password reset features
        builder.Services.AddRedisOtpCacheService(builder.Configuration);

        // Add MassTransit with Saga for registration workflow
        builder.Services.AddMassTransitWithSaga<AuthService.Infrastructure.Context.AuthDbContext>(
            builder.Configuration, 
            configureSagas: x =>
            {
                // Configure Registration Saga owned by AuthService
                AuthSagaConfiguration.ConfigureRegistrationSaga<AuthDbContext>(x);
            },
            configureConsumers: x =>
            {
                // Register consumers for AuthService
                x.AddConsumer<AuthService.Infrastructure.Consumers.CreateAuthUserConsumer>();
                x.AddConsumer<AuthService.Infrastructure.Consumers.DeleteAuthUserConsumer>();
                x.AddConsumer<AuthService.Infrastructure.Consumers.CreatorApplicationConsumer>();
            },
            configureEndpoints: (cfg, context) =>
            {
                // Configure saga-specific endpoints
                AuthSagaConfiguration.ConfigureSagaEndpoints(cfg, context);
            });

        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for AuthService
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("AuthService");

        return app;
    }
}