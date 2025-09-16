using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using ProductAuthMicroservice.Commons.Enums;

namespace AuthService.Infrastructure.Extensions;

/// <summary>
/// Seeding extension specific to AuthService for Identity data
/// </summary>
public static class AuthSeedingExtension
{
    /// <summary>
    /// Seed initial authentication data (roles and admin user) for AuthService
    /// </summary>
    public static async Task SeedAuthDataAsync(this IApplicationBuilder app, ILogger logger)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Check if seeding is enabled
        var enableSeeding = configuration.GetSection("DataConfig").GetValue<bool>("EnableSeeding");
        if (!enableSeeding)
        {
            logger.LogInformation("AuthService: Data seeding is disabled.");
            return;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        logger.LogInformation("AuthService: Starting authentication data seeding...");

        // Seed roles
        await SeedRolesAsync(roleManager, logger);

        // Seed admin user
        await SeedAdminUserAsync(userManager, configuration, logger);

        logger.LogInformation("AuthService: Authentication data seeding completed.");
    }

    /// <summary>
    /// Seed authentication roles from RoleEnum
    /// </summary>
    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager, ILogger logger)
    {
        logger.LogInformation("AuthService: Seeding authentication roles...");

        foreach (var roleName in Enum.GetNames(typeof(RoleEnum)))
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new AppRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Status = EntityStatusEnum.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    logger.LogWarning("AuthService: Failed to create role {Role}: {Errors}", 
                        roleName, errors);
                }
                else
                {
                    logger.LogInformation("AuthService: Created role: {Role}", roleName);
                }
            }
            else
            {
                logger.LogInformation("AuthService: Role {Role} already exists", roleName);
            }
        }
    }

    /// <summary>
    /// Seed admin user for AuthService
    /// </summary>
    private static async Task SeedAdminUserAsync(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        logger.LogInformation("AuthService: Seeding admin user...");

        var adminEmail = configuration.GetSection("DefaultAdminAccount").GetValue<string>("Email")?.Trim();
        var adminPassword = configuration.GetSection("DefaultAdminAccount").GetValue<string>("Password");

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("AuthService: DefaultAdminAccount configuration is missing. Skipping admin seeding.");
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                JoiningAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = EntityStatusEnum.Active
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogWarning("AuthService: Failed to create admin user: {Errors}", errors);
                return;
            }
            existingAdmin = admin;
            logger.LogInformation("AuthService: Created admin user {Email}", adminEmail);
        }
        else
        {
            logger.LogInformation("AuthService: Admin user {Email} already exists", adminEmail);
        }

        // Ensure admin is in Admin role
        var adminRoleName = RoleEnum.Admin.ToString();
        if (!await userManager.IsInRoleAsync(existingAdmin, adminRoleName))
        {
            var addRoleResult = await userManager.AddToRoleAsync(existingAdmin, adminRoleName);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                logger.LogWarning("AuthService: Failed to add admin user to role {Role}: {Errors}", 
                    adminRoleName, errors);
            }
            else
            {
                logger.LogInformation("AuthService: Added admin user to role {Role}", adminRoleName);
            }
        }

        // Final verification
        var roles = await userManager.GetRolesAsync(existingAdmin);
        logger.LogInformation("AuthService: Admin user {Email} roles: {Roles}", 
            existingAdmin.Email, string.Join(", ", roles));
    }
}
