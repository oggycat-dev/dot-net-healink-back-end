using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserService.Infrastructure.Context;
using SharedLibrary.Commons.Extensions;

namespace UserService.Infrastructure.Extensions;

/// <summary>
/// UserService-specific migration extension
/// </summary>
public static class UserMigrationExtension
{
    /// <summary>
    /// Apply UserDbContext migrations for UserService
    /// </summary>
    public static async Task ApplyUserMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("UserService: Starting UserDbContext migrations...");
            
            // Apply migrations for UserDbContext specifically
            await app.ApplyMigrationsAsync<UserDbContext>(logger, "UserService");
            
            logger.LogInformation("UserService: UserDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserService: Failed to apply UserDbContext migrations");
            throw;
        }
    }
}