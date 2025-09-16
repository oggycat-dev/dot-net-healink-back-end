using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AuthService.Infrastructure.Context;
using ProductAuthMicroservice.Commons.Extensions;

namespace AuthService.Infrastructure.Extensions;

/// <summary>
/// AuthService-specific migration extension
/// </summary>
public static class AuthMigrationExtension
{
    /// <summary>
    /// Apply AuthDbContext migrations for AuthService
    /// </summary>
    public static async Task ApplyAuthMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("AuthService: Starting AuthDbContext migrations...");
            
            // Apply migrations for AuthDbContext specifically
            await app.ApplyMigrationsAsync<AuthDbContext>(logger, "AuthService");
            
            logger.LogInformation("AuthService: AuthDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthService: Failed to apply AuthDbContext migrations");
            throw;
        }
    }
}