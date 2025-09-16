using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Commons.Configs;
using ProductService.Infrastructure.Context;

namespace ProductService.Infrastructure;

public static class ProductInfrastructureDependencyInjection
{
    /// <summary>
    /// Add infrastructure services for ProductService
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
        services.AddDbContext<ProductDbContext>(options =>
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
                npgsqlOptions.MigrationsAssembly(typeof(ProductDbContext).Assembly.FullName);
            });
        });

        // Add HTTP Client Factory for CurrentUserService
        services.AddHttpClient();
        
        // Add shared repositories with Outbox support (with specific DbContext)
        services.AddScoped<ProductAuthMicroservice.Commons.Repositories.IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<ProductDbContext>();
            return new ProductAuthMicroservice.Commons.Repositories.UnitOfWork(context);
        });
        
        // Register OutboxUnitOfWork for ProductService
        services.AddScoped<ProductAuthMicroservice.Commons.Outbox.IOutboxUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<ProductDbContext>();
            var eventBus = provider.GetRequiredService<ProductAuthMicroservice.Commons.EventBus.IEventBus>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProductAuthMicroservice.Commons.Outbox.OutboxUnitOfWork>>();
            return new ProductAuthMicroservice.Commons.Outbox.OutboxUnitOfWork(context, eventBus, logger);
        });
        
        // Add product-specific infrastructure services
        // services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
