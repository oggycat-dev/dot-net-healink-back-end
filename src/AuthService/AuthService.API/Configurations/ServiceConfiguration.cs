using AuthService.Application;
using AuthService.Infrastructure;
using ProductAuthMicroservice.Commons.DependencyInjection;

namespace AuthService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for AuthService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services
        builder.ConfigureMicroserviceServices("AuthService");

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
