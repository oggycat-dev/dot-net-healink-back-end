using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductAuthMicroservice.Commons.Configs;

namespace ProductAuthMicroservice.Commons.Configurations;

/// <summary>
/// Extension methods for JWT configuration across microservices
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// Configure JWT authentication
    /// </summary>
    public static IServiceCollection AddJwtConfiguration(this IServiceCollection services, 
        IConfiguration configuration, bool requireHttps = false)
    {
        // Register JwtConfig as IOptions<JwtConfig> for DI
        services.Configure<JwtConfig>(configuration.GetSection(JwtConfig.SectionName));
        
        // Get JWT settings from configuration
        var jwtConfig = configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>();
        
        if (jwtConfig == null)
            throw new ArgumentNullException(nameof(jwtConfig), "JWT configuration section not found");

        if (string.IsNullOrEmpty(jwtConfig.Key))
            throw new ArgumentNullException(nameof(jwtConfig.Key), "JWT Key is not configured");

        if (string.IsNullOrEmpty(jwtConfig.Issuer))
            throw new ArgumentNullException(nameof(jwtConfig.Issuer), "JWT Issuer is not configured");

        if (string.IsNullOrEmpty(jwtConfig.Audience))
            throw new ArgumentNullException(nameof(jwtConfig.Audience), "JWT Audience is not configured");

        // Add JWT authentication
        var key = Encoding.UTF8.GetBytes(jwtConfig.Key);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = requireHttps;
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
            
            // Add event handlers for JWT bearer events
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Customize challenge response if needed
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Additional token validation logic if needed
                    return Task.CompletedTask;
                }
            };
        });
        
        return services;
    }
    
    /// <summary>
    /// Configure JWT with custom token validation parameters
    /// </summary>
    public static IServiceCollection AddJwtConfiguration(this IServiceCollection services,
        JwtConfig jwtConfig, 
        TokenValidationParameters? customValidationParameters = null,
        bool requireHttps = false)
    {
        if (jwtConfig == null)
            throw new ArgumentNullException(nameof(jwtConfig));

        var key = Encoding.UTF8.GetBytes(jwtConfig.Key);
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = requireHttps;
            
            options.TokenValidationParameters = customValidationParameters ?? new TokenValidationParameters
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
        
        return services;
    }
}
