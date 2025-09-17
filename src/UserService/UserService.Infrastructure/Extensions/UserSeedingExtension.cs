using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Context;
using SharedLibrary.Commons.Enums;

namespace UserService.Infrastructure.Extensions;

/// <summary>
/// Seeding extension for UserService to initialize business data
/// </summary>
public static class UserSeedingExtension
{
    /// <summary>
    /// Seed initial user data for UserService
    /// </summary>
    public static async Task SeedUserDataAsync(this IApplicationBuilder app, ILogger logger)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Check if seeding is enabled
        var enableSeeding = configuration.GetSection("DataConfig").GetValue<bool>("EnableSeedData");
        if (!enableSeeding)
        {
            logger.LogInformation("UserService: Data seeding is disabled.");
            return;
        }

        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        logger.LogInformation("UserService: Starting user data seeding...");

        // Seed business roles
        await SeedBusinessRolesAsync(context, logger);

        // Seed admin user profile
        await SeedAdminUserProfileAsync(context, configuration, logger);

        logger.LogInformation("UserService: User data seeding completed.");
    }

    /// <summary>
    /// Seed business roles based on BusinessRoleEnum
    /// </summary>
    private static async Task SeedBusinessRolesAsync(UserDbContext context, ILogger logger)
    {
        logger.LogInformation("UserService: Seeding business roles...");

        var businessRoles = new List<(BusinessRoleEnum RoleType, string Name, string DisplayName, string Description, RoleEnum RequiredCoreRole, bool RequiresApproval, string[] Permissions, int Priority)>
        {
            // User Roles
            (BusinessRoleEnum.FreeUser, "FreeUser", "Free User", "Standard user with basic access", RoleEnum.User, false, new string[] { }, 100),
            (BusinessRoleEnum.PremiumUser, "PremiumUser", "Premium User", "Premium subscriber with enhanced features", RoleEnum.User, false, new string[] { "content.premium.access" }, 90),

            // Content Creator Roles  
            (BusinessRoleEnum.ContentCreator, "ContentCreator", "Content Creator", "Create and manage own content", RoleEnum.User, true, new string[] { "content.create", "content.edit", "content.publish" }, 50),
            (BusinessRoleEnum.ContentEditor, "ContentEditor", "Content Editor", "Edit and review content from creators", RoleEnum.Staff, false, new string[] { "content.create", "content.edit", "content.publish", "content.review" }, 40),
            (BusinessRoleEnum.ExpertCollaborator, "ExpertCollaborator", "Expert Collaborator", "Medical/wellness expert contributing content", RoleEnum.User, true, new string[] { "content.create", "content.edit", "content.expert_review" }, 45),

            // Community Roles
            (BusinessRoleEnum.CommunityMember, "CommunityMember", "Community Member", "Active community participant", RoleEnum.User, false, new string[] { "content.community_story.create", "content.comment" }, 80),
            (BusinessRoleEnum.CommunityModerator, "CommunityModerator", "Community Moderator", "Moderate community content and discussions", RoleEnum.Staff, false, new string[] { "content.moderate", "content.community_story.moderate" }, 30),

            // Admin Roles
            (BusinessRoleEnum.SystemAdministrator, "SystemAdministrator", "System Administrator", "Full system administration access", RoleEnum.Admin, false, new string[] { "system.admin", "system.settings.manage", "system.logs.view" }, 10),
            (BusinessRoleEnum.UserManager, "UserManager", "User Manager", "Manage user accounts and business roles", RoleEnum.Admin, false, new string[] { "user.profile.view", "user.profile.edit", "user.business_roles.manage", "user.applications.approve" }, 20),
            (BusinessRoleEnum.EcommerceManager, "EcommerceManager", "E-commerce Manager", "Manage e-commerce operations", RoleEnum.Admin, false, new string[] { "order.manage", "subscription.manage" }, 25),
            (BusinessRoleEnum.MarketingManager, "MarketingManager", "Marketing Manager", "Manage marketing campaigns and content promotion", RoleEnum.Admin, false, new string[] { "content.promote", "notification.send", "notification.manage" }, 25),
            (BusinessRoleEnum.DataAnalyst, "DataAnalyst", "Data Analyst", "Access to analytics and reporting", RoleEnum.Staff, false, new string[] { "system.monitoring.view", "content.analytics.view", "user.analytics.view" }, 35),
            (BusinessRoleEnum.BusinessOwner, "BusinessOwner", "Business Owner", "Complete business oversight and decision making", RoleEnum.Admin, false, new string[] { "system.admin", "system.monitoring.view", "user.profile.view", "content.analytics.view" }, 5)
        };

        foreach (var (roleType, name, displayName, description, requiredCoreRole, requiresApproval, permissions, priority) in businessRoles)
        {
            if (!await context.BusinessRoles.AnyAsync(br => br.RoleType == roleType))
            {
                var businessRole = new BusinessRole
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    RoleType = roleType,
                    RequiredCoreRole = requiredCoreRole,
                    RequiresApproval = requiresApproval,
                    Permissions = System.Text.Json.JsonSerializer.Serialize(permissions),
                    IsActive = true,
                    Priority = priority,
                    CreatedAt = DateTime.UtcNow,
                    Status = EntityStatusEnum.Active
                };

                context.BusinessRoles.Add(businessRole);
                logger.LogInformation("UserService: Created business role: {Role}", displayName);
            }
            else
            {
                logger.LogInformation("UserService: Business role {Role} already exists", displayName);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("UserService: Business roles seeding completed");
    }

    /// <summary>
    /// Seed admin user profile
    /// </summary>
    private static async Task SeedAdminUserProfileAsync(UserDbContext context, IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("UserService: Seeding admin user profile...");

        var adminEmail = configuration.GetSection("DefaultAdminAccount").GetValue<string>("Email");
        var adminUserId = configuration.GetSection("DefaultAdminAccount").GetValue<Guid>("UserId");

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogWarning("UserService: Admin email configuration is missing. Skipping admin profile seeding.");
            return;
        }


        // Check if admin profile already exists
        var existingProfile = await context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == adminUserId || up.Email == adminEmail);
        if (existingProfile == null)
        {
            var adminProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId, // This should match the admin user created in AuthService
                FullName = "System Administrator",
                Email = adminEmail,
                PhoneNumber = "+1-000-000-0000",
                Address = "System Address",
                AvatarPath = null,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId, // Self-created
                Status = EntityStatusEnum.Active
            };

            context.UserProfiles.Add(adminProfile);
            await context.SaveChangesAsync();

            logger.LogInformation("UserService: Created admin user profile for {Email}", adminEmail);

            // Assign admin business roles
            await AssignAdminBusinessRolesAsync(context, adminProfile.UserId, logger);
        }
        else
        {
            logger.LogInformation("UserService: Admin user profile for {Email} already exists", adminEmail);
            
            // Make sure admin has proper business roles assigned
            await AssignAdminBusinessRolesAsync(context, existingProfile.UserId, logger);
        }
    }

    /// <summary>
    /// Assign business roles to admin user
    /// </summary>
    private static async Task AssignAdminBusinessRolesAsync(UserDbContext context, Guid userId, ILogger logger)
    {
        logger.LogInformation("UserService: Assigning business roles to admin user...");

        // Admin should have these business roles
        var adminBusinessRoles = new[]
        {
            BusinessRoleEnum.SystemAdministrator,
            BusinessRoleEnum.UserManager,
            BusinessRoleEnum.BusinessOwner
        };

        foreach (var roleType in adminBusinessRoles)
        {
            var businessRole = await context.BusinessRoles.FirstOrDefaultAsync(br => br.RoleType == roleType);
            if (businessRole == null)
            {
                logger.LogWarning("UserService: Business role {Role} not found, skipping assignment", roleType);
                continue;
            }

            var existingAssignment = await context.UserBusinessRoles
                .AnyAsync(ubr => ubr.UserId == userId && ubr.BusinessRoleId == businessRole.Id);

            if (!existingAssignment)
            {
                var userBusinessRole = new UserBusinessRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    BusinessRoleId = businessRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = userId, // Self-assigned for initial admin
                    ExpiresAt = null, // Permanent assignment
                    Notes = "Initial admin role assignment during system seeding",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    Status = EntityStatusEnum.Active
                };

                context.UserBusinessRoles.Add(userBusinessRole);
                logger.LogInformation("UserService: Assigned business role {Role} to admin", businessRole.DisplayName);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("UserService: Admin business role assignment completed");
    }
}
