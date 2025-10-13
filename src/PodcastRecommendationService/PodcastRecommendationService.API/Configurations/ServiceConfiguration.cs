using PodcastRecommendationService.Application;
using PodcastRecommendationService.Infrastructure;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Extensions;

namespace PodcastRecommendationService.API.Configurations;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for PodcastRecommendationService microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure microservice with shared services (includes env + logging)
        builder.ConfigureMicroserviceServices("PodcastRecommendationService");

        // Add distributed authentication
        builder.Services.AddMicroserviceDistributedAuth(builder.Configuration);

        // Application & Infrastructure layers
        builder.Services.AddPodcastRecommendationApplication();
        builder.Services.AddPodcastRecommendationInfrastructure(builder.Configuration);

        // Add controllers with JSON options
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
            });

        // Note: Swagger is already configured by ConfigureMicroserviceServices in SharedLibrary
        // No need to call AddSwaggerGen here to avoid duplicate SwaggerDoc "v1"
        
        // Add health checks for AI service dependency
        builder.Services.AddHealthChecks()
            .AddCheck("podcast-ai-service", () =>
            {
                // Simple health check - more sophisticated checks can be implemented
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("AI Service dependency check");
            });

        // Add CORS if needed
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", policy =>
            {
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:3000", "http://localhost:5173" };
                
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return builder;
    }

    /// <summary>
    /// Configure the HTTP request pipeline
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Configure shared pipeline
        app.ConfigureSharedPipeline("PodcastRecommendationService");

        // Development-specific middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Podcast Recommendation Service API v1");
                c.RoutePrefix = string.Empty; // Swagger at root
            });
        }

        // CORS
        app.UseCors("AllowedOrigins");

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        // Health checks
        app.MapHealthChecks("/health");

        return app;
    }
}