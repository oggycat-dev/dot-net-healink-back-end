using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.EventHandlers;
using ProductAuthMicroservice.Commons.BackgroundServices;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Shared.Contracts.Events;
using ProductAuthMicroservice.Commons.Configurations;

namespace ProductAuthMicroservice.Commons.Extensions;

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
        
        // Register event handlers
        services.AddTransient<UserLoggedInEventHandler>();
        services.AddTransient<UserLoggedOutEventHandler>();
        services.AddTransient<UserStatusChangedEventHandler>();
        services.AddTransient<UserRolesChangedEventHandler>();
        services.AddTransient<RefreshTokenRevokedEventHandler>();
        services.AddTransient<UserSessionsInvalidatedEventHandler>();
        
        // Register background service for user state sync
        services.AddHostedService<UserStateSyncService>();
        
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
