using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using ProductAuthMicroservice.Commons.Configs;

namespace ProductAuthMicroservice.Commons.Configurations;

/// <summary>
/// Configuration for Cross-Origin Resource Sharing (CORS) across microservices
/// </summary>
public static class CorsConfiguration
{
    private const string DefaultCorsPolicy = "DefaultCorsPolicy";
    private const string ProductionCorsPolicy = "ProductionCorsPolicy";
    private const string DevelopmentCorsPolicy = "DevelopmentCorsPolicy";
    
    /// <summary>
    /// Configure CORS with environment-specific policies
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, 
        IConfiguration configuration, bool allowAnyOrigin = false)
    {
        // Extract CORS configuration from appsettings
        var corsConfig = configuration.GetSection("Cors").Get<CorsConfig>() ?? new CorsConfig();
        
        // Get current environment
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetService<IHostEnvironment>();
        var isProduction = environment?.IsProduction() ?? false;

        services.AddCors(options =>
        {
            // Default policy (for backward compatibility)
            options.AddPolicy(DefaultCorsPolicy, policy =>
            {
                ConfigurePolicy(policy, corsConfig, isProduction, allowAnyOrigin);
            });

            // Production-specific policy with strict security
            options.AddPolicy(ProductionCorsPolicy, policy =>
            {
                ConfigureProductionPolicy(policy, corsConfig);
            });

            // Development-specific policy with looser security
            options.AddPolicy(DevelopmentCorsPolicy, policy =>
            {
                ConfigureDevelopmentPolicy(policy, corsConfig);
            });
        });
        
        return services;
    }

    // Helper method to configure a policy based on environment
    private static void ConfigurePolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, 
        CorsConfig corsConfig, bool isProduction, bool allowAnyOrigin)
    {
        // Configure origins
        if (allowAnyOrigin || corsConfig.AllowAnyOrigin)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            if (corsConfig.AllowedOrigins.Length == 0)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(corsConfig.AllowedOrigins);
            }
        }
        
        // Configure methods
        if (corsConfig.AllowAnyMethod)
        {
            policy.AllowAnyMethod();
        }
        else if (corsConfig.AllowedMethods.Length > 0)
        {
            policy.WithMethods(corsConfig.AllowedMethods);
        }
        else
        {
            policy.AllowAnyMethod();
        }
        
        // Configure headers
        if (corsConfig.AllowAnyHeader)
        {
            policy.AllowAnyHeader();
        }
        else if (corsConfig.AllowedHeaders.Length > 0)
        {
            policy.WithHeaders(corsConfig.AllowedHeaders);
        }
        else
        {
            policy.AllowAnyHeader();
        }
        
        // Configure exposed headers
        if (corsConfig.ExposedHeaders.Length > 0)
        {
            policy.WithExposedHeaders(corsConfig.ExposedHeaders);
        }
        
        // Configure credentials
        if (corsConfig.AllowCredentials)
        {
            policy.AllowCredentials();
        }
        
        // Configure preflight max age
        if (corsConfig.PreflightMaxAgeSeconds.HasValue)
        {
            policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.PreflightMaxAgeSeconds.Value));
        }
    }
    
    private static void ConfigureProductionPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, 
        CorsConfig corsConfig)
    {
        if (corsConfig.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsConfig.AllowedOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }
        
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition", "Token-Expired")
            .SetIsOriginAllowedToAllowWildcardSubdomains();
            
        if (corsConfig.PreflightMaxAgeSeconds.HasValue)
        {
            policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.PreflightMaxAgeSeconds.Value));
        }
    }

    private static void ConfigureDevelopmentPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy, 
        CorsConfig corsConfig)
    {
        if (corsConfig.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsConfig.AllowedOrigins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }
        
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition", "Token-Expired");
            
        if (corsConfig.PreflightMaxAgeSeconds.HasValue)
        {
            policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.PreflightMaxAgeSeconds.Value));
        }
    }
    
    /// <summary>
    /// Use the environment-appropriate CORS policy
    /// </summary>
    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app)
    {
        // Get current environment
        var environment = app.ApplicationServices.GetService<IHostEnvironment>();
        
        // Use the appropriate policy based on environment
        if (environment?.IsProduction() == true)
        {
            app.UseCors(ProductionCorsPolicy);
        }
        else
        {
            app.UseCors(DevelopmentCorsPolicy);
        }
        
        return app;
    }
    
    /// <summary>
    /// Use a specific CORS policy by name
    /// </summary>
    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app, string policyName)
    {
        app.UseCors(policyName);
        return app;
    }
}
