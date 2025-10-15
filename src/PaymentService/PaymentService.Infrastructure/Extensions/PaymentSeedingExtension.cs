using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Enums;

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
            
            // Seed PaymentMethods
            await SeedPaymentMethodsAsync(context, logger, configuration);
            
            logger.LogInformation("PaymentService: Payment data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentService: Error occurred during payment data seeding");
            throw;
        }
    }
    
    private static async Task SeedPaymentMethodsAsync(PaymentDbContext context, ILogger logger, IConfiguration configuration)
    {
        try
        {
            logger.LogInformation("PaymentService: Checking PaymentMethods to seed...");

            // ✅ Get admin user ID from configuration (same as AuthService)
            var adminUserId = configuration.GetSection("DefaultAdminAccount").GetValue<Guid>("UserId");
            if (adminUserId == Guid.Empty)
            {
                logger.LogWarning("PaymentService: DefaultAdminAccount:UserId not configured. Using system default.");
                adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            }

            var now = DateTime.UtcNow;
            var seededCount = 0;

            // ✅ Define payment methods to seed
            var paymentMethodsToSeed = new List<(string ProviderName, string Name, string Description, PaymentType Type, EntityStatusEnum Status)>
            {
                (
                    nameof(PaymentGatewayType.Momo),
                    "Thanh Toán bằng Momo",
                    "Đây là thanh toán bằng Momo",
                    PaymentType.EWallet,
                    EntityStatusEnum.Active
                ),
                (
                    nameof(PaymentGatewayType.VnPay),
                    "Thanh Toán bằng VnPay",
                    "Thanh toán qua cổng VnPay",
                    PaymentType.EWallet,
                    EntityStatusEnum.Inactive
                ),
                (
                    nameof(PaymentGatewayType.PayPal),
                    "Thanh Toán bằng PayPal",
                    "Thanh toán quốc tế qua PayPal",
                    PaymentType.CreditCard,
                    EntityStatusEnum.Inactive
                ),
                (
                    "Cash",
                    "Thanh Toán Tiền Mặt",
                    "Thanh toán bằng tiền mặt khi giao hàng",
                    PaymentType.Cash,
                    EntityStatusEnum.Inactive
                ),
                (
                    "BankTransfer",
                    "Chuyển Khoản Ngân Hàng",
                    "Chuyển khoản trực tiếp qua ngân hàng",
                    PaymentType.BankTransfer,
                    EntityStatusEnum.Inactive
                )
            };

            // ✅ Check and seed each payment method by ProviderName
            foreach (var (providerName, name, description, type, status) in paymentMethodsToSeed)
            {
                // Check if payment method with this ProviderName already exists
                var existingMethod = await context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.ProviderName == providerName);

                if (existingMethod != null)
                {
                    logger.LogInformation(
                        "PaymentService: PaymentMethod with ProviderName '{ProviderName}' already exists (ID: {Id}). Skipping...",
                        providerName, existingMethod.Id);
                    continue;
                }

                // Seed new payment method
                var paymentMethod = new PaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    Type = type,
                    ProviderName = providerName,
                    Configuration = "{}",
                    Status = status,
                    CreatedAt = now,
                    CreatedBy = adminUserId,
                    IsDeleted = false
                };

                await context.PaymentMethods.AddAsync(paymentMethod);
                seededCount++;

                logger.LogInformation(
                    "PaymentService: Created PaymentMethod: {Name} (Provider: {Provider}, Status: {Status})",
                    name, providerName, status);
            }

            if (seededCount > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation(
                    "PaymentService: Successfully seeded {Count} new PaymentMethods",
                    seededCount);
            }
            else
            {
                logger.LogInformation("PaymentService: All PaymentMethods already exist. No seeding required.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentService: Error seeding PaymentMethods");
            throw;
        }
    }
}