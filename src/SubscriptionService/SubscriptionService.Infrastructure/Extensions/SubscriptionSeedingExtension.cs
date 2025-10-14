using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SubscriptionService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Entities;

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
            
            // Get settings from configuration
            var freePlanName = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanName") ?? "Free";
            var freePlanAmount = configuration.GetValue<decimal>("SubscriptionSettings:DefaultFreePlanAmount");
            var currency = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanCurrency") ?? "VND";
            var trialDays = configuration.GetValue<int>("SubscriptionSettings:DefaultTrialDays");
            var adminUserId = configuration.GetValue<Guid?>("AdminUserId");
            
            // Define subscription plans to seed
            var plansToSeed = new List<(string Name, string DisplayName, string Description, decimal Amount, string Currency, 
                SubscriptionService.Domain.Enums.BillingPeriodUnit BillingPeriodUnit, int BillingPeriodCount, int TrialDays, string FeatureConfig)>
            {
                (
                    freePlanName,
                    freePlanName,
                    "Free plan with basic features",
                    freePlanAmount,
                    currency,
                    SubscriptionService.Domain.Enums.BillingPeriodUnit.Month,
                    1,
                    0,
                    "{}"
                ),
                (
                    "Premium",
                    "Premium",
                    "Premium plan with advanced features",
                    50000m,
                    currency,
                    SubscriptionService.Domain.Enums.BillingPeriodUnit.Month,
                    1,
                    trialDays,
                    "{\"maxContent\": 1000, \"premium\": true}"
                ),
                (
                    "yearly-premium",
                    "Yearly Premium",
                    "Yearly Premium for user",
                    200000m,
                    currency,
                    SubscriptionService.Domain.Enums.BillingPeriodUnit.Year,
                    12,
                    0,
                    "{\"maxContent\": 1000, \"premium\": true, \"yearlyDiscount\": true}"
                )
            };
            
            // Check and add only plans that don't exist
            var plansAdded = 0;
            
            logger.LogInformation("SubscriptionService: Checking SubscriptionPlans to seed...");
            
            foreach (var (name, displayName, description, amount, curr, billingUnit, billingCount, trial, featureConfig) in plansToSeed)
            {
                // Check if plan exists by exact name match
                var existingPlan = await context.SubscriptionPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name == name);
                
                if (existingPlan != null)
                {
                    logger.LogInformation($"SubscriptionService: SubscriptionPlan '{name}' already exists (ID: {existingPlan.Id}). Skipping...");
                    continue;
                }
                
                // Create new plan
                var newPlan = new SubscriptionService.Domain.Entities.SubscriptionPlan
                {
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    Amount = amount,
                    Currency = curr,
                    BillingPeriodUnit = billingUnit,
                    BillingPeriodCount = billingCount,
                    TrialDays = trial,
                    FeatureConfig = featureConfig,
                    Status = EntityStatusEnum.Active
                };
                
                // Use EntityExtension for consistency
                newPlan.InitializeEntity(adminUserId);
                
                await context.SubscriptionPlans.AddAsync(newPlan);
                plansAdded++;
                
                logger.LogInformation($"SubscriptionService: Created SubscriptionPlan: {name} (Amount: {amount} {curr}, Status: Active)");
            }
            
            if (plansAdded > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation($"SubscriptionService: Successfully seeded {plansAdded} new SubscriptionPlan(s)");
            }
            else
            {
                logger.LogInformation("SubscriptionService: All SubscriptionPlans already exist. No seeding required.");
            }
            
            logger.LogInformation("SubscriptionService: Subscription data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SubscriptionService: Error occurred during subscription data seeding");
            throw;
        }
    }
}
