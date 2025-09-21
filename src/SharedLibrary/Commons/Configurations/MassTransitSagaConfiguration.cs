using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Contracts.User.Saga;

namespace SharedLibrary.Commons.Configurations;

/// <summary>
/// Extension methods for MassTransit Saga configuration
/// </summary>
public static class MassTransitSagaConfiguration
{
    /// <summary>
    /// Add MassTransit with Saga configuration for AuthService
    /// </summary>
    public static IServiceCollection AddMassTransitWithSaga<TDbContext>(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IRegistrationConfigurator>? configureConsumers = null,
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
            // Add Registration Saga with optimized configuration
            x.AddSagaStateMachine<RegistrationSaga, RegistrationSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<TDbContext>();
                    r.UsePostgres();
                    
                    // CRITICAL: Use pessimistic concurrency for data integrity
                    // Prevents duplicate key violations during concurrent saga creation
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    
                    // ReadCommitted isolation - Serializable was causing deadlocks/retries
                    // This allows proper saga creation without blocking on concurrent access
                    r.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
                });

            // Configure consumers (e.g., for AuthService consumers)
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

                // Disable global retry for AuthService - prioritize data integrity
                cfg.UseMessageRetry(r => r.None());
                
                // TODO: Configure proper message scheduler when RabbitMQ delayed message plugin is available
                // For now, remove scheduling to avoid plugin dependency
                
                // Configure endpoints
                cfg.ConfigureEndpoints(context);
                
                // Configure saga endpoint with proper fault handling
                cfg.ReceiveEndpoint("registration-saga", e =>
                {
                    e.ConfigureSaga<RegistrationSagaState>(context);
                    
                    // CRITICAL: Disable ALL retry mechanisms
                    e.UseMessageRetry(r => r.None());
                    
                    // CRITICAL: Handle faults without retrying
                    e.DiscardFaultedMessages();
                    e.DiscardSkippedMessages();
                    
                    // Single threaded processing to prevent race conditions
                    e.ConcurrentMessageLimit = 1;
                    
                    // Minimal prefetch to reduce duplicate processing  
                    e.PrefetchCount = 1;
                });
            });
        });

        return services;
    }

    /// <summary>
    /// Add MassTransit with consumers for NotificationService and UserService
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