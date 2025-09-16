using ProductService.Application;
using ProductService.Infrastructure;
using ProductAuthMicroservice.Commons.DependencyInjection;

namespace ProductService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for ProductService microservice (internal service behind Gateway)
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes RabbitMQ EventBus)
        builder.ConfigureMicroserviceServices("ProductService");

        // Application & Infrastructure layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for ProductService (internal service)
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("ProductService");
        
        return app;
    }
}
