using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using UserService.Application;
using UserService.Infrastructure;

namespace UserService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for UserService microservice (internal service behind Gateway)
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes RabbitMQ EventBus)
        builder.ConfigureMicroserviceServices("UserService");

        // Add MassTransit with consumers for User creation workflow
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
        {
            x.AddConsumer<UserService.Infrastructure.Consumers.CreateUserProfileConsumer>();
            x.AddConsumer<UserService.Infrastructure.Consumers.DeleteUserProfileConsumer>();
        });

        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for UserService (internal service)
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("UserService");
        
        return app;
    }
}