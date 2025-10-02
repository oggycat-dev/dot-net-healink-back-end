using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Extensions;

/// <summary>
/// Seeding extension specific to PaymentService for payment configuration
/// </summary>
public static class PaymentSeedingExtension
{
    /// <summary>
    /// Seed initial payment configuration for PaymentService
    /// </summary>
    public static async Task SeedPaymentDataAsync(this IApplicationBuilder app, ILogger logger)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Check if seeding is enabled
        var enableSeeding = configuration.GetSection("DataConfig").GetValue<bool>("EnableSeeding");
        if (!enableSeeding)
        {
            logger.LogInformation("PaymentService: Data seeding is disabled.");
            return;
        }

        try
        {
            logger.LogInformation("PaymentService: Starting payment data seeding...");
            
            var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            
            // PaymentService doesn't need initial data seeding like subscription plans
            // This is placeholder for future payment method configurations or similar
            logger.LogInformation("PaymentService: No initial data seeding required at this time");
            
            logger.LogInformation("PaymentService: Payment data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentService: Error occurred during payment data seeding");
            throw;
        }
    }
}