using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.DependencyInjection;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Middlewares;
using SharedLibrary.Commons.Services;

namespace Gateway.API.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all services for Gateway
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Load environment configuration first
        builder.AddEnvironmentConfiguration("Gateway");
        
        // Add logging configuration
        builder.AddLoggingConfiguration("Gateway");

        // Core ASP.NET services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Gateway-specific configurations (without JWT - we handle it manually below)
        builder.Services.AddCorsConfiguration(builder.Configuration);
        
        // Add Gateway distributed auth (different from microservice auth)
        builder.Services.AddGatewayDistributedAuth(builder.Configuration);
        
        // Register JWT configuration
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection(JwtConfig.SectionName));
        
        // Add current user service
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("AuthService", client =>
        {
            client.BaseAddress = new Uri("http://authservice-api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Configure Ocelot
        builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

        // Configure Authentication for Ocelot with specific scheme name
        var jwtConfig = builder.Configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>();
        
        if (jwtConfig != null && !string.IsNullOrEmpty(jwtConfig.Key))
        {
            var key = System.Text.Encoding.UTF8.GetBytes(jwtConfig.Key);
            
            builder.Services.AddAuthentication()
                .AddJwtBearer("Bearer", options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwtConfig.ValidateIssuer,
                        ValidateAudience = jwtConfig.ValidateAudience,
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                        ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = jwtConfig.ValidateLifetime,
                        ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes)
                    };
                });
        }

        builder.Services.AddOcelot(builder.Configuration);
        builder.Services.AddAuthorization();

        return builder;
    }

    /// <summary>
    /// Configure the middleware pipeline for Gateway
    /// </summary>
    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        var environment = app.Environment;
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        
        logger.LogInformation("Configuring Gateway pipeline for Ocelot, Environment: {Environment}", environment.EnvironmentName);
        
        // Enable Swagger for development
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        // Use CORS - must be before authentication and Ocelot
        app.UseCorsConfiguration();

        // Use correlation ID middleware for distributed tracing - early in pipeline
        app.UseCorrelationId();

        // Use distributed auth middleware (Gateway-specific) - before Ocelot
        app.UseMiddleware<Gateway.API.Middlewares.DistributedAuthMiddleware>();

        // Use Ocelot - handles its own auth pipeline
        await app.UseOcelot();

        return app;
    }
}
