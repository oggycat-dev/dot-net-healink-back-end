using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Context;
using SharedLibrary.Commons.Extensions;

namespace PaymentService.Infrastructure.Extensions;

/// <summary>
/// PaymentService-specific migration extension
/// </summary>
public static class PaymentMigrationExtension
{
    /// <summary>
    /// Apply PaymentDbContext migrations for PaymentService
    /// </summary>
    public static async Task ApplyPaymentMigrationsAsync(this IApplicationBuilder app, ILogger logger)
    {
        try
        {
            logger.LogInformation("PaymentService: Starting PaymentDbContext migrations...");
            
            // Apply migrations for PaymentDbContext specifically
            await app.ApplyMigrationsAsync<PaymentDbContext>(logger, "PaymentService");
            
            logger.LogInformation("PaymentService: PaymentDbContext migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentService: Failed to apply PaymentDbContext migrations");
            throw;
        }
    }
}