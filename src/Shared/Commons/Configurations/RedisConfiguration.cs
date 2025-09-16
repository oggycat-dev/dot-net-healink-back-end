using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Configs;
using StackExchange.Redis;

namespace ProductAuthMicroservice.Commons.Configurations;

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
        
        // Register IUserStateCache as Singleton for middleware compatibility
        // Redis connections are thread-safe and can be shared across requests
        services.AddSingleton<IUserStateCache, RedisUserStateCache>();

        return services;
    }
}