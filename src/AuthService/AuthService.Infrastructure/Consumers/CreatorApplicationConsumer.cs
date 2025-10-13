using AuthService.Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Contracts.User.Events;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// Consumer to update user roles in both database and Redis cache when Creator Application is approved
/// This ensures immediate role propagation without requiring re-login
/// </summary>
public class CreatorApplicationConsumer : IConsumer<CreatorApplicationApprovedEvent>
{
    private readonly ILogger<CreatorApplicationConsumer> _logger;
    private readonly IUserStateCache _userStateCache;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public CreatorApplicationConsumer(
        ILogger<CreatorApplicationConsumer> logger,
        IUserStateCache userStateCache,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _logger = logger;
        _userStateCache = userStateCache;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task Consume(ConsumeContext<CreatorApplicationApprovedEvent> context)
    {
        var approvedEvent = context.Message;
        
        _logger.LogInformation(
            "AuthService received CreatorApplicationApprovedEvent for User: {UserId} with Role: {Role}",
            approvedEvent.UserId, approvedEvent.BusinessRoleName);

        try
        {
            // Step 1: Update user role in DATABASE first
            var user = await _userManager.FindByIdAsync(approvedEvent.UserId.ToString());
            
            if (user == null)
            {
                _logger.LogWarning(
                    "User not found in database for UserId: {UserId}. Cannot add ContentCreator role.",
                    approvedEvent.UserId);
                return;
            }

            // Check if ContentCreator role exists in database
            var roleExists = await _roleManager.RoleExistsAsync("ContentCreator");
            if (!roleExists)
            {
                _logger.LogError(
                    "ContentCreator role does not exist in database. Please seed roles first.");
                return;
            }

            // Add ContentCreator role to user in database if not exists
            var isInRole = await _userManager.IsInRoleAsync(user, "ContentCreator");
            if (!isInRole)
            {
                var result = await _userManager.AddToRoleAsync(user, "ContentCreator");
                
                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "✅ Successfully added ContentCreator role to DATABASE for User: {UserId}",
                        approvedEvent.UserId);
                }
                else
                {
                    _logger.LogError(
                        "❌ Failed to add ContentCreator role to DATABASE for User: {UserId}. Errors: {Errors}",
                        approvedEvent.UserId,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return;
                }
            }
            else
            {
                _logger.LogInformation(
                    "ContentCreator role already exists in DATABASE for User: {UserId}",
                    approvedEvent.UserId);
            }

            // Step 2: Update Redis cache for immediate access (without re-login)
            var userState = await _userStateCache.GetUserStateAsync(approvedEvent.UserId);
            
            if (userState == null)
            {
                _logger.LogWarning(
                    "User state not found in Redis cache for UserId: {UserId}. " +
                    "Role added to database, user will get it on next login.",
                    approvedEvent.UserId);
                return;
            }

            // Add ContentCreator role to cache if not exists
            var userRoles = userState.Roles.ToList();
            
            if (!userRoles.Contains("ContentCreator"))
            {
                userRoles.Add("ContentCreator");
                
                var updatedState = userState with { Roles = userRoles };
                await _userStateCache.SetUserStateAsync(updatedState);
                
                _logger.LogInformation(
                    "✅ Successfully added ContentCreator role to REDIS CACHE for User: {UserId}. " +
                    "User can now upload content immediately without re-login.",
                    approvedEvent.UserId);
                
                _logger.LogDebug(
                    "Updated roles for User {UserId}: {Roles}",
                    approvedEvent.UserId,
                    string.Join(", ", userRoles));
            }
            else
            {
                _logger.LogInformation(
                    "ContentCreator role already exists in REDIS CACHE for User: {UserId}",
                    approvedEvent.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ Error updating user roles for UserId: {UserId}. " +
                "Please check database and cache connectivity.",
                approvedEvent.UserId);
            
            // Don't throw - this is not critical enough to fail the message
            // User can still re-login to get the new role
        }
    }
}
