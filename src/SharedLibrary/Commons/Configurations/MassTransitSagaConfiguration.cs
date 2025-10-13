using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.Configs;

namespace SharedLibrary.Commons.Configurations;

/// <summary>
/// Generic extension methods for MassTransit Saga configuration
/// Each microservice should configure its own saga state machines
/// </summary>
public static class MassTransitSagaConfiguration
{
    /// <summary>
    /// Add MassTransit with generic Saga configuration
    /// Use this when you need to configure saga state machines in your service
    /// </summary>
    /// <typeparam name="TDbContext">DbContext that will store saga state</typeparam>
    /// <param name="configureSagas">Action to configure saga state machines</param>
    /// <param name="configureConsumers">Action to configure consumers</param>
    /// <param name="configureEndpoints">Action to configure custom endpoints (optional)</param>
    public static IServiceCollection AddMassTransitWithSaga<TDbContext>(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IRegistrationConfigurator> configureSagas,
        Action<IRegistrationConfigurator>? configureConsumers = null,
        Action<IRabbitMqBusFactoryConfigurator, IBusRegistrationContext>? configureEndpoints = null,
        string connectionStringKey = "DefaultConnection")
        where TDbContext : DbContext
    {
        var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
        
        // Sử dụng ConnectionConfig thay vì GetConnectionString để tương thích với infrastructure
        var connectionConfig = configuration.GetSection("ConnectionConfig").Get<ConnectionConfig>();
        var connectionString = connectionConfig?.DefaultConnection;
        
        if (rabbitMQConfig == null)
        {
            throw new ArgumentNullException(nameof(rabbitMQConfig), "RabbitMQ configuration not found");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Database connection string not found in ConnectionConfig");
        }

        services.AddMassTransit(x =>
        {
            // Configure sagas - each service defines its own
            configureSagas(x);

            // Configure consumers (optional)
            configureConsumers?.Invoke(x);

            // Configure RabbitMQ transport
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMQConfig.HostName, rabbitMQConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMQConfig.UserName);
                    h.Password(rabbitMQConfig.Password);
                    
                    if (rabbitMQConfig.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.ServerName = rabbitMQConfig.SslServerName;
                        });
                    }
                });

                // Default retry configuration (can be overridden per endpoint)
                cfg.UseMessageRetry(r => r.Interval(rabbitMQConfig.RetryCount, TimeSpan.FromSeconds(rabbitMQConfig.RetryDelaySeconds)));
                
                // Configure endpoints - auto-configure first, then custom
                cfg.ConfigureEndpoints(context);
                
                // Apply custom endpoint configuration if provided
                configureEndpoints?.Invoke(cfg, context);
            });
        });

        return services;
    }

    /// <summary>
    /// Add MassTransit with consumers and optional Entity Framework Outbox
    /// </summary>
    /// <typeparam name="TDbContext">Optional DbContext for Entity Framework Outbox</typeparam>
    public static IServiceCollection AddMassTransitWithConsumers<TDbContext>(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IRegistrationConfigurator>? configureConsumers = null,
        bool useEntityFrameworkOutbox = false,
        bool useBusOutbox = false)
        where TDbContext : DbContext
    {
        var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
        
        if (rabbitMQConfig == null)
        {
            throw new ArgumentNullException(nameof(rabbitMQConfig), "RabbitMQ configuration not found");
        }

        services.AddMassTransit(x =>
        {
            // Configure consumers
            configureConsumers?.Invoke(x);

            // CRITICAL: Add Entity Framework Outbox if requested
            // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
            if (useEntityFrameworkOutbox)
            {
                x.AddEntityFrameworkOutbox<TDbContext>(o =>
                {
                    // Use PostgreSQL lock provider for pessimistic locking
                    o.UsePostgres();

                    // Enable bus outbox for HTTP handler publishes (IPublishEndpoint)
                    if (useBusOutbox)
                    {
                        o.UseBusOutbox();
                    }

                    // Duplicate detection window (inbox deduplication)
                    o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
                    
                    // Query settings for outbox delivery service
                    o.QueryDelay = TimeSpan.FromSeconds(1);
                    o.QueryMessageLimit = 100;
                });
            }

            // Configure RabbitMQ transport
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMQConfig.HostName, rabbitMQConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMQConfig.UserName);
                    h.Password(rabbitMQConfig.Password);
                    
                    if (rabbitMQConfig.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.ServerName = rabbitMQConfig.SslServerName;
                        });
                    }
                });

                // Configure message retry
                cfg.UseMessageRetry(r => r.Interval(rabbitMQConfig.RetryCount, TimeSpan.FromSeconds(rabbitMQConfig.RetryDelaySeconds)));
                
                // Configure endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
    
    /// <summary>
    /// Add MassTransit with consumers (without DbContext/Outbox)
    /// For services that don't need transactional outbox
    /// </summary>
    public static IServiceCollection AddMassTransitWithConsumers(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
        
        if (rabbitMQConfig == null)
        {
            throw new ArgumentNullException(nameof(rabbitMQConfig), "RabbitMQ configuration not found");
        }

        services.AddMassTransit(x =>
        {
            // Configure consumers
            configureConsumers?.Invoke(x);

            // Configure RabbitMQ transport
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMQConfig.HostName, rabbitMQConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMQConfig.UserName);
                    h.Password(rabbitMQConfig.Password);
                    
                    if (rabbitMQConfig.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.ServerName = rabbitMQConfig.SslServerName;
                        });
                    }
                });

                // Configure message retry
                cfg.UseMessageRetry(r => r.Interval(rabbitMQConfig.RetryCount, TimeSpan.FromSeconds(rabbitMQConfig.RetryDelaySeconds)));
                
                // Configure endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}