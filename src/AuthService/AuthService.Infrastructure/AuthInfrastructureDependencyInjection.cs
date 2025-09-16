using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.DependencyInjection;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using AuthService.Application.Commons.Interfaces;
using AuthService.Infrastructure.Services;
using ProductAuthMicroservice.Commons.Configurations;

namespace AuthService.Infrastructure;

public static class AuthInfrastructureDependencyInjection
{
    /// <summary>
    /// Add infrastructure services for AuthService
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        var connectionConfig = configuration.GetSection("ConnectionConfig").Get<ConnectionConfig>();
        if (connectionConfig == null || string.IsNullOrEmpty(connectionConfig.DefaultConnection))
        {
            throw new InvalidOperationException("AuthService database connection string not found");
        }

        // Configure AuthDbContext with PostgreSQL
        services.AddDbContext<AuthDbContext>(options =>
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
                npgsqlOptions.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
            });
        });

        // Configure Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // For development
        })
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        // Add shared repositories with Outbox support (with specific DbContext)
        services.AddScoped<ProductAuthMicroservice.Commons.Repositories.IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<AuthDbContext>();
            return new ProductAuthMicroservice.Commons.Repositories.UnitOfWork(context);
        });
        
        // Register OutboxUnitOfWork for AuthService
        services.AddScoped<ProductAuthMicroservice.Commons.Outbox.IOutboxUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<AuthDbContext>();
            var eventBus = provider.GetRequiredService<ProductAuthMicroservice.Commons.EventBus.IEventBus>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProductAuthMicroservice.Commons.Outbox.OutboxUnitOfWork>>();
            return new ProductAuthMicroservice.Commons.Outbox.OutboxUnitOfWork(context, eventBus, logger);
        });
        
        // Add Memory Cache for Token Blocklist
        services.AddMemoryCache();
        
        // Add Token Blocklist Service
        
        // Add HTTP Client Factory for CurrentUserService
        services.AddHttpClient();
        
        // Add Current User Service
        services.AddScoped<ProductAuthMicroservice.Commons.Services.ICurrentUserService, ProductAuthMicroservice.Commons.Services.CurrentUserService>();
        services.AddHttpContextAccessor();
        
        // Add auth-specific infrastructure services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuthJwtService, AuthJwtService>();
        
        return services;
    }
}
