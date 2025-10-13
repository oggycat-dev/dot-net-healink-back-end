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
            
            // Get settings from configuration
            var freePlanName = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanName") ?? "Free";
            var freePlanAmount = configuration.GetValue<decimal>("SubscriptionSettings:DefaultFreePlanAmount");
            var currency = configuration.GetValue<string>("SubscriptionSettings:DefaultFreePlanCurrency") ?? "VND";
            var trialDays = configuration.GetValue<int>("SubscriptionSettings:DefaultTrialDays");
            var adminUserId = configuration.GetValue<Guid?>("AdminUserId");
            
            // Define subscription plans to seed
            var plansToSeed = new[]
            {
                new SubscriptionService.Domain.Entities.SubscriptionPlan
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
                    Status = EntityStatusEnum.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId
                },
                new SubscriptionService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium",
                    DisplayName = "Premium",
                    Description = "Premium plan with advanced features",
                    Amount = 50000,
                    Currency = currency,
                    BillingPeriodUnit = SubscriptionService.Domain.Enums.BillingPeriodUnit.Month,
                    BillingPeriodCount = 1,
                    TrialDays = trialDays,
                    FeatureConfig = "{\"maxContent\": 1000, \"premium\": true}",
                    Status = EntityStatusEnum.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId
                },
                new SubscriptionService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "yearly-premium",
                    DisplayName = "Yearly Premium",
                    Description = "Yearly Premium for user",
                    Amount = 200000,
                    Currency = currency,
                    BillingPeriodUnit = SubscriptionService.Domain.Enums.BillingPeriodUnit.Year,
                    BillingPeriodCount = 12,
                    TrialDays = 0,
                    FeatureConfig = "{\"maxContent\": 1000, \"premium\": true, \"yearlyDiscount\": true}",
                    Status = EntityStatusEnum.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId
                }
            };
            
            // Check and add only plans that don't exist
            var plansAdded = 0;
            foreach (var plan in plansToSeed)
            {
                var existingPlan = await context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Name == plan.Name);
                
                if (existingPlan == null)
                {
                    context.SubscriptionPlans.Add(plan);
                    plansAdded++;
                    logger.LogInformation($"SubscriptionService: Adding plan '{plan.Name}'");
                }
                else
                {
                    logger.LogInformation($"SubscriptionService: Plan '{plan.Name}' already exists, skipping");
                }
            }
            
            if (plansAdded > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation($"SubscriptionService: Successfully added {plansAdded} new subscription plan(s)");
            }
            else
            {
                logger.LogInformation("SubscriptionService: No new plans to add, all plans already exist");
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
