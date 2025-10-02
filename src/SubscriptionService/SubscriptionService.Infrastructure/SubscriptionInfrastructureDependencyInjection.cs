using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Configs;
using SubscriptionService.Infrastructure.Context;

namespace SubscriptionService.Infrastructure;

public static class SubscriptionInfrastructureDependencyInjection
{
    /// <summary>
    /// Add infrastructure services for SubscriptionService
    /// </summary>
    public static IServiceCollection AddSubscriptionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        var connectionConfig = configuration.GetSection("ConnectionConfig").Get<ConnectionConfig>();
        if (connectionConfig == null || string.IsNullOrEmpty(connectionConfig.DefaultConnection))
        {
            throw new InvalidOperationException("SubscriptionService database connection string not found");
        }

        // Configure SubscriptionDbContext with PostgreSQL
        services.AddDbContext<SubscriptionDbContext>(options =>
        {
            options.UseNpgsql(connectionConfig.DefaultConnection, npgsqlOptions =>
            {
                if (connectionConfig.RetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: connectionConfig.MaxRetryCount > 0 ? connectionConfig.MaxRetryCount : 3,
                        maxRetryDelay: TimeSpan.FromSeconds(connectionConfig.MaxRetryDelay > 0 ? connectionConfig.MaxRetryDelay : 30),
                        errorCodesToAdd: null);
                }
                npgsqlOptions.MigrationsAssembly(typeof(SubscriptionDbContext).Assembly.FullName);
            });
        });

        // Add shared repositories with Outbox support (with specific DbContext)
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<SubscriptionDbContext>();
            return new UnitOfWork(context);
        });

        // Register OutboxUnitOfWork for SubscriptionService
        services.AddScoped<IOutboxUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<SubscriptionDbContext>();
            var eventBus = provider.GetRequiredService<IEventBus>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxUnitOfWork>>();
            return new OutboxUnitOfWork(context, eventBus, logger);
        });

        // Add HTTP Client Factory for CurrentUserService
        services.AddHttpClient();

        // Add Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Add Redis cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // MassTransit consumers are automatically registered via AddMassTransitWithSaga

        return services;
    }
}
