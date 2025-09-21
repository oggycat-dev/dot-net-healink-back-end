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
        string connectionStringKey = "DefaultConnection")
        where TDbContext : DbContext
    {
        var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
        var connectionString = configuration.GetConnectionString(connectionStringKey);
        
        if (rabbitMQConfig == null)
        {
            throw new ArgumentNullException(nameof(rabbitMQConfig), "RabbitMQ configuration not found");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Database connection string not found");
        }

        services.AddMassTransit(x =>
        {
            // Add Registration Saga
            x.AddSagaStateMachine<RegistrationSaga, RegistrationSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<TDbContext>();
                    r.UsePostgres();
                });

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
                
                // Configure saga endpoint
                cfg.ReceiveEndpoint("registration-saga", e =>
                {
                    e.ConfigureSaga<RegistrationSagaState>(context);
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
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

    /// <summary>
    /// Add Database configuration for Saga persistence
    /// </summary>
    public static void AddSagaTables(this ModelBuilder modelBuilder)
    {
        // Configure RegistrationSagaState table
        modelBuilder.Entity<RegistrationSagaState>(entity =>
        {
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.EncryptedPassword).HasMaxLength(512);
            entity.Property(x => x.FullName).HasMaxLength(256);
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.OtpCode).HasMaxLength(10);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.Channel).HasConversion<string>();
            
            entity.Property(x => x.AuthUserId);
            entity.Property(x => x.UserProfileId);
            entity.Property(x => x.AuthUserCreatedAt);
            entity.Property(x => x.UserProfileCreatedAt);
            
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.CurrentState);
        });
    }
}