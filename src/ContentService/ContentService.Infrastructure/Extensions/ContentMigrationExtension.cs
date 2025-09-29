using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ContentService.Infrastructure.Context;
using SharedLibrary.Commons.Extensions;

namespace ContentService.Infrastructure.Extensions;

/// <summary>
/// ContentService-specific migration extension
/// </summary>
public static class ContentMigrationExtension
{
    /// <summary>
    /// Apply ContentDbContext migrations for ContentService
    /// </summary>
    public static async Task ApplyContentMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("ContentService: Starting ContentDbContext migrations...");
            
            // Apply migrations for ContentDbContext specifically
            await app.ApplyMigrationsAsync<ContentDbContext>(logger, "ContentService");
            
            logger.LogInformation("ContentService: ContentDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ContentService: Failed to apply ContentDbContext migrations");
            throw;
        }
    }
}
