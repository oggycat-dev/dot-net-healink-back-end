using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SubscriptionService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Enums;

namespace SubscriptionService.Infrastructure.Extensions;

/// <summary>
/// Seeding extension specific to SubscriptionService for subscription plans
/// </summary>
public static class SubscriptionSeedingExtension
{
    /// <summary>
    /// Seed initial subscription plans for SubscriptionService
    /// </summary>
    public static async Task SeedSubscriptionDataAsync(this IApplicationBuilder app, ILogger logger)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Check if seeding is enabled
        var enableSeeding = configuration.GetSection("DataConfig").GetValue<bool>("EnableSeeding");
        if (!enableSeeding)
        {
            logger.LogInformation("SubscriptionService: Data seeding is disabled.");
            return;
        }

        try
        {
            logger.LogInformation("SubscriptionService: Starting subscription data seeding...");
            
            var context = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
            
            // Check if any plans exist
            if (await context.SubscriptionPlans.AnyAsync())
            {
                logger.LogInformation("SubscriptionService: Subscription plans already exist, skipping seeding");
                return;
            }
            
            // Get settings from configuration
            var freePlanName = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanName") ?? "Free";
            var freePlanAmount = configuration.GetValue<decimal>("SubscriptionSettings:DefaultFreePlanAmount");
            var currency = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanCurrency") ?? "USD";
            var trialDays = configuration.GetValue<int>("SubscriptionSettings:DefaultTrialDays");
            var adminUserId = configuration.GetValue<Guid?>("AdminUserId");
            
            // Create default subscription plans
            var freePlan = new SubscriptionService.Domain.Entities.SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = freePlanName,
                DisplayName = freePlanName,
                Description = "Free plan with basic features",
                Amount = freePlanAmount,
                Currency = currency,
                BillingPeriodUnit = SubscriptionService.Domain.Enums.BillingPeriodUnit.Month,
                BillingPeriodCount = 1,
                TrialDays = 0,
                FeatureConfig = "{}",
                Status = EntityStatusEnum.Active, // ✅ Using Status instead of IsActive
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId
            };
            
            var premiumPlan = new SubscriptionService.Domain.Entities.SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Premium",
                DisplayName = "Premium",
                Description = "Premium plan with advanced features",
                Amount = 9.99m,
                Currency = currency,
                BillingPeriodUnit = SubscriptionService.Domain.Enums.BillingPeriodUnit.Month,
                BillingPeriodCount = 1,
                TrialDays = trialDays,
                FeatureConfig = "{\"maxContent\": 1000, \"premium\": true}",
                Status = EntityStatusEnum.Active, // ✅ Using Status instead of IsActive
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId
            };
            
            context.SubscriptionPlans.AddRange(freePlan, premiumPlan);
            await context.SaveChangesAsync();
            
            logger.LogInformation("SubscriptionService: Subscription data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SubscriptionService: Error occurred during subscription data seeding");
            throw;
        }
    }
}
