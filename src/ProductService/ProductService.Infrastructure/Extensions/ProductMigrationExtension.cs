using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductService.Infrastructure.Context;
using ProductAuthMicroservice.Commons.Extensions;

namespace ProductService.Infrastructure.Extensions;

/// <summary>
/// ProductService-specific migration extension
/// </summary>
public static class ProductMigrationExtension
{
    /// <summary>
    /// Apply ProductDbContext migrations for ProductService
    /// </summary>
    public static async Task ApplyProductMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("ProductService: Starting ProductDbContext migrations...");
            
            // Apply migrations for ProductDbContext specifically
            await app.ApplyMigrationsAsync<ProductDbContext>(logger, "ProductService");
            
            logger.LogInformation("ProductService: ProductDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProductService: Failed to apply ProductDbContext migrations");
            throw;
        }
    }
}