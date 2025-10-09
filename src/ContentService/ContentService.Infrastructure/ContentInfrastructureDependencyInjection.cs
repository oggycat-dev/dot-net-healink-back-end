using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ContentService.Infrastructure.Context;
using ContentService.Infrastructure.Services;
using ContentService.Infrastructure.Repositories;
using ContentService.Infrastructure.Consumers;
using ContentService.Domain.Interfaces;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.EventBus;

namespace ContentService.Infrastructure;

public static class ContentInfrastructureDependencyInjection
{
    public static IServiceCollection AddContentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration["ConnectionConfig:DefaultConnection"] ?? 
                              configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Outbox pattern for reliable messaging
        services.AddScoped<IOutboxUnitOfWork, OutboxUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<ContentDbContext>();
            var eventBus = provider.GetService<IEventBus>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxUnitOfWork>>();
            return new OutboxUnitOfWork(context, eventBus, logger);
        });

        // Base repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<ContentDbContext>();
            return new UnitOfWork(context);
        });

        // Background services
        services.AddHostedService<OutboxEventProcessorService>();

        // Note: IFileStorageService is registered in SharedLibrary via AddS3FileStorage()
        // Do not register here to avoid overriding the SharedLibrary implementation

        // Repositories
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();

            // Register MassTransit consumers
            services.AddScoped<UserEventConsumer>();
            services.AddScoped<AuthEventConsumer>();
            services.AddScoped<CreatorApplicationConsumer>();

        return services;
    }
}