using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Commons.EventHandlers;
using SharedLibrary.Commons.BackgroundServices;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.SharedLibrary.Contracts.Events;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Services;

namespace SharedLibrary.Commons.Extensions;

/// <summary>
/// Extension methods để configure distributed authentication và authorization
/// </summary>
public static class DistributedAuthExtensions
{
    /// <summary>
    /// Add distributed authentication services
    /// </summary>
    public static IServiceCollection AddDistributedAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register cache configuration
        services.Configure<CacheConfig>(configuration.GetSection(CacheConfig.SectionName));
        
        // Register memory cache
        services.AddMemoryCache(options =>
        {
            var cacheConfig = configuration.GetSection(CacheConfig.SectionName).Get<CacheConfig>() ?? new CacheConfig();
            options.SizeLimit = cacheConfig.MaxCacheSize;
        });
        
        // Register Redis user state cache (includes Redis configuration)
        // Using Singleton for middleware compatibility
        services.AddRedisUserStateCache(configuration);
        
        // Register event handlers with both concrete type and interface
        services.AddTransient<UserLoggedInEventHandler>();
        services.AddTransient<IIntegrationEventHandler<UserLoggedInEvent>>(sp => sp.GetRequiredService<UserLoggedInEventHandler>());
        
        services.AddTransient<UserLoggedOutEventHandler>();
        services.AddTransient<IIntegrationEventHandler<UserLoggedOutEvent>>(sp => sp.GetRequiredService<UserLoggedOutEventHandler>());
        
        services.AddTransient<UserStatusChangedEventHandler>();
        services.AddTransient<IIntegrationEventHandler<UserStatusChangedEvent>>(sp => sp.GetRequiredService<UserStatusChangedEventHandler>());
        
        services.AddTransient<UserRolesChangedEventHandler>();
        services.AddTransient<IIntegrationEventHandler<UserRolesChangedEvent>>(sp => sp.GetRequiredService<UserRolesChangedEventHandler>());
        
        services.AddTransient<RefreshTokenRevokedEventHandler>();
        services.AddTransient<IIntegrationEventHandler<RefreshTokenRevokedEvent>>(sp => sp.GetRequiredService<RefreshTokenRevokedEventHandler>());
        
        services.AddTransient<UserSessionsInvalidatedEventHandler>();
        services.AddTransient<IIntegrationEventHandler<UserSessionsInvalidatedEvent>>(sp => sp.GetRequiredService<UserSessionsInvalidatedEventHandler>());
        
        // Register background service for user state sync only if Redis is enabled
        var redisConfig = configuration.GetSection("Redis").Get<RedisConfig>();
        if (redisConfig?.Enabled == true)
        {
            services.AddHostedService<UserStateSyncService>();
        }
        
        return services;
    }
    
    /// <summary>
    /// Subscribe to authentication events
    /// </summary>
    public static void SubscribeToAuthEvents(this IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        
        // Subscribe to user authentication events
        eventBus.Subscribe<UserLoggedInEvent, UserLoggedInEventHandler>();
        eventBus.Subscribe<UserLoggedOutEvent, UserLoggedOutEventHandler>();
        eventBus.Subscribe<UserStatusChangedEvent, UserStatusChangedEventHandler>();
        eventBus.Subscribe<UserRolesChangedEvent, UserRolesChangedEventHandler>();
        eventBus.Subscribe<RefreshTokenRevokedEvent, RefreshTokenRevokedEventHandler>();
        eventBus.Subscribe<UserSessionsInvalidatedEvent, UserSessionsInvalidatedEventHandler>();
        
        // Start consuming events
        eventBus.StartConsuming();
    }
    
    /// <summary>
    /// Configure distributed authentication in Gateway
    /// </summary>
    public static IServiceCollection AddGatewayDistributedAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register cache configuration
        services.Configure<CacheConfig>(configuration.GetSection(CacheConfig.SectionName));
        
        // Register memory cache
        services.AddMemoryCache(options =>
        {
            var cacheConfig = configuration.GetSection(CacheConfig.SectionName).Get<CacheConfig>() ?? new CacheConfig();
            options.SizeLimit = cacheConfig.MaxCacheSize;
        });
        
        // Register Redis user state cache (includes Redis configuration)
        services.AddRedisUserStateCache(configuration);
        
        // Register JWT service for token validation (singleton for middleware)
        services.AddSingleton<IJwtService, JwtService>();
        
        // Gateway doesn't need event handlers or background services
        // It only needs JWT authentication and user state cache
        
        return services;
    }
    
    /// <summary>
    /// Configure distributed authentication in microservices
    /// </summary>
    public static IServiceCollection AddMicroserviceDistributedAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddDistributedAuthentication(configuration);
    }
}
