using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Configs;
using StackExchange.Redis;

namespace SharedLibrary.Commons.Configurations;

/// <summary>
/// Extension methods for Redis configuration
/// </summary>
public static class RedisConfiguration
{
    /// <summary>
    /// Add Redis distributed cache configuration
    /// </summary>
    public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register Redis configuration
        services.Configure<RedisConfig>(configuration.GetSection(RedisConfig.SectionName));
        
        var redisConfig = configuration.GetSection(RedisConfig.SectionName).Get<RedisConfig>();
        
        if (redisConfig == null || string.IsNullOrEmpty(redisConfig.ConnectionString))
        {
            throw new ArgumentNullException(nameof(redisConfig), "Redis configuration not found or connection string is empty");
        }

        // Add Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfig.ConnectionString;
            options.InstanceName = redisConfig.InstanceName;
        });

        // Add Redis connection multiplexer for advanced scenarios
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = ConfigurationOptions.Parse(redisConfig.ConnectionString);
            configuration.ConnectTimeout = redisConfig.ConnectTimeout;
            configuration.SyncTimeout = redisConfig.SyncTimeout;
            configuration.AbortOnConnectFail = redisConfig.AbortOnConnectFail;
            configuration.ConnectRetry = redisConfig.ConnectRetry;
            
            return ConnectionMultiplexer.Connect(configuration);
        });

        return services;
    }

    /// <summary>
    /// Add Redis-based UserStateCache
    /// </summary>
    public static IServiceCollection AddRedisUserStateCache(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add Redis configuration first
        services.AddRedisConfiguration(configuration);
        
        // Register IUserStateCache as Scoped - Redis connections are thread-safe
        services.AddScoped<IUserStateCache, RedisUserStateCache>();

        return services;
    }

    /// <summary>
    /// Add Redis-based OtpCacheService
    /// </summary>
    public static IServiceCollection AddRedisOtpCacheService(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add Redis configuration first
        services.AddRedisConfiguration(configuration);
        
        // Register IOtpCacheService as Scoped for per-request use
        services.AddScoped<IOtpCacheService, OtpCacheService>();

        return services;
    }

    // /// <summary>
    // /// Add all Redis-based cache services (UserState + OTP)
    // /// </summary>
    // public static IServiceCollection AddRedisCacheServices(this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     // Add Redis configuration first
    //     services.AddRedisConfiguration(configuration);
        
    //     // Register cache services - both as Scoped for consistency
    //     services.AddScoped<IUserStateCache, RedisUserStateCache>();
    //     services.AddScoped<IOtpCacheService, OtpCacheService>();

    //     return services;
    // }
}