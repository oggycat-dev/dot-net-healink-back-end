using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Commons.Cache;

namespace SharedLibrary.Commons.Configurations;

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
            
            // Add event handlers for JWT bearer events with Redis cache sync
            // This ensures roles from cache are synced to JWT claims on every request
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
                OnTokenValidated = async context =>
                {
                    // Sync roles from Redis cache to JWT claims
                    // This allows immediate role updates without requiring re-login
                    try
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        
                        var userStateCache = context.HttpContext.RequestServices
                            .GetRequiredService<IUserStateCache>();

                        // Get user ID from JWT claims
                        var userIdClaim = context.Principal?.FindFirst("user_id")
                            ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)
                            ?? context.Principal?.FindFirst("sub")
                            ?? context.Principal?.FindFirst("userId");

                        if (userIdClaim == null) return;

                        var userId = userIdClaim.Value;
                        
                        // Parse userId to Guid
                        if (!Guid.TryParse(userId, out var userGuid))
                        {
                            return; // Invalid userId format
                        }
                        
                        // Get latest user state from Redis cache
                        var userState = await userStateCache.GetUserStateAsync(userGuid);
                        if (userState == null) return;

                        // Get existing roles from JWT
                        var jwtRoles = context.Principal?.FindAll(ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToHashSet() ?? new HashSet<string>();

                        // Get roles from Redis cache
                        var cacheRoles = userState.Roles.ToHashSet();

                        // Add new roles from cache that aren't in JWT
                        var newRoles = cacheRoles.Except(jwtRoles).ToList();
                        
                        if (newRoles.Any())
                        {
                            logger.LogInformation(
                                "🔄 Syncing {Count} new roles from cache for User: {UserId}. New roles: {Roles}",
                                newRoles.Count, userId, string.Join(", ", newRoles));

                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            
                            if (identity != null)
                            {
                                foreach (var role in newRoles)
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                }

                                logger.LogInformation(
                                    "✅ Successfully synced roles from cache. User {UserId} now has roles: {Roles}",
                                    userId, string.Join(", ", cacheRoles));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Don't fail authentication if cache sync fails
                        // User will still have roles from JWT
                    }
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
