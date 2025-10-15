using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;

namespace AuthService.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();

        try
        {
            logger.LogInformation("Starting role seeding...");

            var roles = new[]
            {
                new { Name = "User", Description = "Regular user role" },
                new { Name = "ContentCreator", Description = "Content creator role for users approved to create podcasts" },
                new { Name = "Admin", Description = "Administrator role with full system access" },
                new { Name = "Moderator", Description = "Moderator role for content management" }
            };

            foreach (var roleInfo in roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleInfo.Name);
                if (!roleExists)
                {
                    var role = new AppRole
                    {
                        Name = roleInfo.Name,
                        NormalizedName = roleInfo.Name.ToUpperInvariant(),
                        Status = EntityStatusEnum.Active,
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("✅ Created role: {RoleName} (ID: {RoleId})", roleInfo.Name, role.Id);
                    }
                    else
                    {
                        logger.LogError("❌ Failed to create role {RoleName}: {Errors}", 
                            roleInfo.Name, 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("ℹ️  Role already exists: {RoleName}", roleInfo.Name);
                }
            }

            logger.LogInformation("Role seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding roles");
            throw;
        }
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();

        try
        {
            logger.LogInformation("Initializing AuthService database...");

            // Apply pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied successfully");
            }
            else
            {
                logger.LogInformation("ℹ️  No pending migrations");
            }

            // Seed roles
            await SeedRolesAsync(serviceProvider);

            logger.LogInformation("✅ AuthService database initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error occurred while initializing database");
            throw;
        }
    }
}
