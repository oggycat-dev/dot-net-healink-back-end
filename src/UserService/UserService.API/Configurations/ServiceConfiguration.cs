using SharedLibrary.Commons.DependencyInjection;
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