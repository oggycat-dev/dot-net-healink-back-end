using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.Configs;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using UserService.Infrastructure.Context;

namespace UserService.Infrastructure;

public static class UserInfrastructureDependencyInjection
{
    /// <summary>
    /// Add infrastructure services for UserService
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        var connectionConfig = configuration.GetSection("ConnectionConfig").Get<ConnectionConfig>();
        if (connectionConfig == null || string.IsNullOrEmpty(connectionConfig.DefaultConnection))
        {
            throw new InvalidOperationException("ProductService database connection string not found");
        }

        // Configure ProductDbContext with PostgreSQL
        services.AddDbContext<UserDbContext>(options =>
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
                npgsqlOptions.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
            });
        });

        // Add HTTP Client Factory for CurrentUserService
        services.AddHttpClient();

        // Add shared repositories with Outbox support (with specific DbContext)
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<UserDbContext>();
            return new UnitOfWork(context);
        });

        // Register OutboxUnitOfWork for ProductService
        services.AddScoped<IOutboxUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<UserDbContext>();
            var eventBus = provider.GetRequiredService<IEventBus>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxUnitOfWork>>();
            return new OutboxUnitOfWork(context, eventBus, logger);
        });

        // Add product-specific infrastructure services
        // services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
