using ContentService.Application;
using ContentService.Infrastructure;
using ContentService.Infrastructure.Consumers;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;

namespace ContentService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for ContentService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("ContentService");

        // Add HttpClient for shared services
        builder.Services.AddHttpClient();

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Add Authorization Policies
        builder.Services.AddAuthorization(options =>
        {
            // ContentCreator policy - requires ContentCreator role
            options.AddPolicy("ContentCreator", policy =>
                policy.RequireRole("ContentCreator", "Expert", "Admin"));

            // CommunityModerator policy - requires moderator role  
            options.AddPolicy("CommunityModerator", policy =>
                policy.RequireRole("CommunityModerator", "Admin"));

            // Admin only policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
        });

        // Add S3 File Storage from SharedLibrary
        builder.Services.AddS3FileStorage(builder.Configuration);

        // Add MassTransit with comprehensive consumers for ContentService
        builder.Services.AddMassTransitWithConsumers(builder.Configuration, x =>
        {
            // User Lifecycle Event Consumers
            x.AddConsumer<UserEventConsumer>();
            
            // Authentication & Authorization Event Consumers  
            x.AddConsumer<AuthEventConsumer>();
            
            // Creator Application Consumers
            x.AddConsumer<CreatorApplicationConsumer>();
        });

        // Application & Infrastructure layers
        builder.Services.AddContentApplication();
        builder.Services.AddContentInfrastructure(builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for ContentService
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Use shared pipeline configuration
        app.ConfigureSharedPipeline("ContentService");

        return app;
    }
}
