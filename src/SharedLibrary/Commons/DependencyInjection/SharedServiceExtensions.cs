using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Middlewares;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Commons.DependencyInjection;

namespace SharedLibrary.Commons.DependencyInjection;

/// <summary>
/// Shared service extensions for microservices
/// </summary>
public static class SharedServiceExtensions
{
    /// <summary>
    /// Add core shared services for microservices
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services, 
        IConfiguration configuration, string serviceName)
    {
        // Add HttpContextAccessor
        services.AddHttpContextAccessor();
        
        // Register current user service for microservices
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Register JWT service for token validation
        services.AddScoped<IJwtService, JwtService>();
        
        // Register role-based access filters
        services.AddScoped<UserRoleAccessFilter>();
        services.AddScoped<StaffRoleAccessFilter>();
        services.AddScoped<AdminRoleAccessFilter>();

        // Register validation configuration
        services.AddValidationConfiguration();

        // Add shared configurations
        services.AddCorsConfiguration(configuration);
        // JWT Configuration is registered separately in ConfigureMicroserviceServices via AddJwtConfiguration
        // Don't register here to avoid duplicate registration
        services.AddSwaggerConfiguration(serviceName);
        
        // Add RabbitMQ Event Bus
        services.AddRabbitMQEventBus(configuration);

        return services;
    }

    /// <summary>
    /// Configure shared middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseSharedMiddleware(this IApplicationBuilder app)
    {
        // Use correlation ID middleware for distributed tracing - must be early
        app.UseCorrelationId();
        
        // Use global exception handling
        app.UseGlobalExceptionHandling();

        // Use JWT middleware
        app.UseJwtMiddleware();

        return app;
    }
    
    /// <summary>
    /// Configure common API pipeline for microservices
    /// </summary>
    public static WebApplication ConfigureSharedPipeline(this WebApplication app, string serviceName)
    {
        var environment = app.Environment;
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        
        logger.LogInformation($"Configuring pipeline for service: {serviceName}, Environment: {environment.EnvironmentName}");
        
        // Always enable Swagger for now - debugging
        logger.LogInformation("About to configure Swagger...");
        app.UseSwaggerConfiguration(serviceName);
        logger.LogInformation("Swagger configuration completed");

        app.UseHttpsRedirection();
        
        // Enable static files for any uploaded content
        app.UseStaticFiles();
        
        // Enable CORS
        app.UseCorsConfiguration();

        // Shared middlewares
        app.UseSharedMiddleware();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}

/// <summary>
/// Application-specific service configuration
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Configure services for a complete microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureMicroserviceServices(this WebApplicationBuilder builder, 
        string serviceName)
    {
        // Load environment configuration first
        builder.AddEnvironmentConfiguration(serviceName);
        
        // Add logging configuration
        builder.AddLoggingConfiguration(serviceName);

        // Core ASP.NET services
        builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Only configure JSON serialization to camelCase - simple!
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });
        builder.Services.AddEndpointsApiExplorer();
        
        // Add JWT authentication with role sync from Redis
        builder.Services.AddJwtConfiguration(builder.Configuration);
        
        // Shared services
        builder.Services.AddSharedServices(builder.Configuration, serviceName);

        return builder;
    }
    
    /// <summary>
    /// Configure services for Gateway microservice
    /// </summary>
    public static WebApplicationBuilder ConfigureGatewayServices(this WebApplicationBuilder builder)
    {
        // Load environment configuration first
        builder.AddEnvironmentConfiguration("Gateway");
        
        // Add logging configuration
        builder.AddLoggingConfiguration("Gateway");

        // Core ASP.NET services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Gateway-specific configurations
        builder.Services.AddCorsConfiguration(builder.Configuration);
        builder.Services.AddJwtConfiguration(builder.Configuration);
        
        // Add Gateway distributed auth (different from microservice auth)
        builder.Services.AddGatewayDistributedAuth(builder.Configuration);
        
        // Add current user service
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("AuthService", client =>
        {
            client.BaseAddress = new Uri("http://authservice-api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
    
    /// <summary>
    /// Configure Gateway-specific pipeline
    /// </summary>
    public static WebApplication ConfigureGatewayPipeline(this WebApplication app)
    {
        var environment = app.Environment;
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        
        logger.LogInformation("Configuring Gateway pipeline, Environment: {Environment}", environment.EnvironmentName);
        
        // Enable Swagger for development
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        // Use CORS - must be before authentication and Ocelot
        app.UseCorsConfiguration();

        // Use shared middleware (includes CorrelationId)
        app.UseSharedMiddleware();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}

/// <summary>
/// Infrastructure service extensions for microservices
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Add database context with PostgreSQL for microservices
    /// </summary>
    public static IServiceCollection AddPostgreSqlContext<TContext>(this IServiceCollection services,
        IConfiguration configuration,
        string? connectionStringName = null)
        where TContext : class
    {
        // This would be implemented with the BaseDbContextFactory pattern
        // Implementation depends on specific DbContext requirements
        return services;
    }
    
    /// <summary>
    /// Add shared repositories and unit of work
    /// </summary>
    public static IServiceCollection AddSharedRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}

/// <summary>
/// Generic database extensions for microservices
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Apply database migrations if enabled
    /// </summary>
    public static async Task<WebApplication> ApplyMigrationsIfEnabledAsync(
        this WebApplication app,
        string serviceName)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger($"{serviceName}Database");
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        try
        {
            var dataConfig = configuration.GetSection("DataConfig").Get<DataConfig>();
            
            if (dataConfig?.EnableAutoApplyMigrations == true)
            {
                logger.LogInformation("{ServiceName}: Applying database migrations...", serviceName);
                
                // Apply migrations using reflection to find the DbContext
                var services = app.Services;
                var dbContextTypes = services.GetServices<DbContext>().Select(c => c.GetType()).ToList();
                
                if (dbContextTypes.Any())
                {
                    var dbContextType = dbContextTypes.First();
                    var method = typeof(MigrationExtension).GetMethod("ApplyMigrationsAsync")?.MakeGenericMethod(dbContextType);
                    if (method != null)
                    {
                        await (Task)method.Invoke(null, new object[] { app, logger, serviceName })!;
                    }
                }
                else
                {
                    logger.LogWarning("{ServiceName}: No DbContext found for migrations", serviceName);
                }
                
                logger.LogInformation("{ServiceName}: Database migrations completed.", serviceName);
            }
            else
            {
                logger.LogInformation("{ServiceName}: Auto-apply migrations is disabled.", serviceName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName}: Failed to apply database migrations", serviceName);
            throw;
        }
        
        return app;
    }
}