using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Enums;

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

        // Seed permissions
        await SeedPermissionsAsync(scope.ServiceProvider, logger);

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
        var adminDefaultUserId = configuration.GetSection("DefaultAdminAccount").GetValue<Guid>("UserId");

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
                Id = adminDefaultUserId,
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
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

    /// <summary>
    /// Seed core permissions for the system
    /// </summary>
    private static async Task SeedPermissionsAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.LogInformation("AuthService: Seeding system permissions...");

        var context = serviceProvider.GetRequiredService<AuthDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

        // Define core permissions based on modules
        var corePermissions = new List<(string Name, string DisplayName, string Description, PermissionModuleEnum Module)>
        {
            // Authentication Module
            ("auth.manage", "Manage Authentication", "Full control over authentication system", PermissionModuleEnum.Authentication),
            ("auth.users.view", "View Users", "View user accounts and profiles", PermissionModuleEnum.Authentication),
            ("auth.users.create", "Create Users", "Create new user accounts", PermissionModuleEnum.Authentication),
            ("auth.users.edit", "Edit Users", "Edit user accounts and profiles", PermissionModuleEnum.Authentication),
            ("auth.users.delete", "Delete Users", "Delete user accounts", PermissionModuleEnum.Authentication),
            ("auth.roles.manage", "Manage Roles", "Manage system roles and permissions", PermissionModuleEnum.Authentication),

            // User Module
            ("user.profile.view", "View User Profiles", "View detailed user profiles", PermissionModuleEnum.User),
            ("user.profile.edit", "Edit User Profiles", "Edit user profiles", PermissionModuleEnum.User),
            ("user.business_roles.manage", "Manage Business Roles", "Assign/revoke business roles", PermissionModuleEnum.User),
            ("user.applications.view", "View Creator Applications", "View creator applications", PermissionModuleEnum.User),
            ("user.applications.approve", "Approve Creator Applications", "Approve/reject creator applications", PermissionModuleEnum.User),

            // Content Module  
            ("content.create", "Create Content", "Create new content", PermissionModuleEnum.Content),
            ("content.edit", "Edit Content", "Edit existing content", PermissionModuleEnum.Content),
            ("content.delete", "Delete Content", "Delete content", PermissionModuleEnum.Content),
            ("content.publish", "Publish Content", "Publish/unpublish content", PermissionModuleEnum.Content),
            ("content.moderate", "Moderate Content", "Moderate community content", PermissionModuleEnum.Content),
            ("content.podcast.manage", "Manage Podcasts", "Full control over podcast content", PermissionModuleEnum.Content),
            ("content.flashcard.manage", "Manage Flashcards", "Full control over flashcard content", PermissionModuleEnum.Content),
            ("content.community_story.moderate", "Moderate Community Stories", "Moderate user-generated stories", PermissionModuleEnum.Content),

            // System Module
            ("system.admin", "System Administration", "Full system administration access", PermissionModuleEnum.System),
            ("system.settings.manage", "Manage System Settings", "Configure system settings", PermissionModuleEnum.System),
            ("system.logs.view", "View System Logs", "Access to system logs", PermissionModuleEnum.System),
            ("system.monitoring.view", "View System Monitoring", "Access to system monitoring", PermissionModuleEnum.System),

            // Notification Module
            ("notification.send", "Send Notifications", "Send notifications to users", PermissionModuleEnum.Notification),
            ("notification.manage", "Manage Notifications", "Manage notification templates and settings", PermissionModuleEnum.Notification)
        };

        // Create permissions
        foreach (var (name, displayName, description, module) in corePermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Name == name))
            {
                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    Module = module,
                    CreatedAt = DateTime.UtcNow,
                    Status = EntityStatusEnum.Active
                };

                context.Permissions.Add(permission);
                logger.LogInformation("AuthService: Created permission: {Permission}", name);
            }
        }

        await context.SaveChangesAsync();

        // Assign all permissions to Admin role
        await AssignPermissionsToAdminRole(context, roleManager, logger);

        logger.LogInformation("AuthService: Permission seeding completed");
    }

    /// <summary>
    /// Assign all permissions to Admin role
    /// </summary>
    private static async Task AssignPermissionsToAdminRole(AuthDbContext context, RoleManager<AppRole> roleManager, ILogger logger)
    {
        logger.LogInformation("AuthService: Assigning permissions to Admin role...");

        var adminRole = await roleManager.FindByNameAsync(RoleEnum.Admin.ToString());
        if (adminRole == null)
        {
            logger.LogWarning("AuthService: Admin role not found, skipping permission assignment");
            return;
        }

        var allPermissions = await context.Permissions.ToListAsync();
        
        foreach (var permission in allPermissions)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
            {
                var rolePermission = new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = EntityStatusEnum.Active
                };

                context.RolePermissions.Add(rolePermission);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("AuthService: Admin role assigned {Count} permissions", allPermissions.Count);
    }
}
