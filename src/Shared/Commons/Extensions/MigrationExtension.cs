using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProductAuthMicroservice.Commons.Extensions;

public static class MigrationExtension
{
    /// <summary>
    /// Auto apply pending migrations for specific DbContext type
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="app">IApplicationBuilder</param>
    /// <param name="logger">ILogger</param>
    /// <param name="serviceName">Service name for logging</param>
    /// <returns>Task</returns>
    public static async Task ApplyMigrationsAsync<TContext>(this IApplicationBuilder app, ILogger logger, string serviceName = "") 
        where TContext : DbContext
    {
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            logger.LogInformation("{ServiceName}: Starting database migrations...", serviceName);

            // Test database connection with retry logic
            try
            {
                await RetryDatabaseConnectionAsync(dbContext, serviceName, logger);
                logger.LogInformation("{ServiceName}: Successfully connected to database.", serviceName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ServiceName}: Failed to connect to database after multiple attempts!", serviceName);
                throw;
            }

            // Apply pending migrations
            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

                logger.LogInformation(
                    "{ServiceName}: Found {PendingCount} pending migrations and {AppliedCount} previously applied migrations",
                    serviceName, pendingMigrations.Count(), appliedMigrations.Count());

                if (pendingMigrations.Any())
                {
                    logger.LogInformation("{ServiceName}: Applying pending migrations: {Migrations}",
                        serviceName, string.Join(", ", pendingMigrations));

                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("{ServiceName}: Successfully applied all pending migrations.", serviceName);
                }
                else
                {
                    logger.LogInformation("{ServiceName}: No pending migrations found for database.", serviceName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ServiceName}: An error occurred while applying migrations!", serviceName);
                throw;
            }

            logger.LogInformation("{ServiceName}: Database migrations completed successfully.", serviceName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName}: A problem occurred during database migrations!", serviceName);
            throw;
        }
    }

    /// <summary>
    /// Test database connection with retry logic
    /// </summary>
    private static async Task RetryDatabaseConnectionAsync(DbContext context, string contextName,
        ILogger logger, int maxRetries = 3, int delaySeconds = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to connect to {ContextName} database (attempt {Attempt}/{MaxRetries})...",
                    contextName, attempt, maxRetries);

                // Test connection
                await context.Database.CanConnectAsync();
                logger.LogInformation("Successfully connected to {ContextName} database on attempt {Attempt}",
                    contextName, attempt);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex,
                    "Failed to connect to {ContextName} database on attempt {Attempt}. Retrying in {DelaySeconds} seconds...",
                    contextName, attempt, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to {ContextName} database after {MaxRetries} attempts",
                    contextName, maxRetries);
                throw;
            }
        }
    }

    /// <summary>
    /// Ensure database is created if it doesn't exist
    /// </summary>
    public static void EnsureDatabaseCreated<TContext>(this IApplicationBuilder app, ILogger logger, string serviceName = "")
        where TContext : DbContext
    {
        try
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            logger.LogInformation("{ServiceName}: Checking if database exists...", serviceName);

            if (dbContext.Database.EnsureCreated())
            {
                logger.LogInformation("{ServiceName}: Database was created successfully.", serviceName);
            }
            else
            {
                logger.LogInformation("{ServiceName}: Database already exists.", serviceName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName}: An error occurred while ensuring database exists!", serviceName);
            throw;
        }
    }
}