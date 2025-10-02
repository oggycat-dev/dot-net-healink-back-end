using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SubscriptionService.Infrastructure.Context;
using SharedLibrary.Commons.Extensions;

namespace SubscriptionService.Infrastructure.Extensions;

/// <summary>
/// SubscriptionService-specific migration extension
/// </summary>
public static class SubscriptionMigrationExtension
{
    /// <summary>
    /// Apply SubscriptionDbContext migrations for SubscriptionService
    /// </summary>
    public static async Task ApplySubscriptionMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("SubscriptionService: Starting SubscriptionDbContext migrations...");
            
            // Apply migrations for SubscriptionDbContext specifically
            await app.ApplyMigrationsAsync<SubscriptionDbContext>(logger, "SubscriptionService");
            
            logger.LogInformation("SubscriptionService: SubscriptionDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SubscriptionService: Failed to apply SubscriptionDbContext migrations");
            throw;
        }
    }
}
