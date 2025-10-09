using AuthService.Application.Commons.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Service for managing user roles in database
/// </summary>
public class RoleService : IRoleService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger<RoleService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task AddRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when trying to add role {RoleName}", userId, roleName);
            return;
        }

        // Check if role exists
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            _logger.LogWarning("Role {RoleName} does not exist", roleName);
            return;
        }

        // Check if user already has the role
        var hasRole = await _userManager.IsInRoleAsync(user, roleName);
        if (hasRole)
        {
            _logger.LogInformation("User {UserId} already has role {RoleName}", userId, roleName);
            return;
        }

        // Add role to user
        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            _logger.LogInformation("Successfully added role {RoleName} to user {UserId}", roleName, userId);
        }
        else
        {
            _logger.LogError("Failed to add role {RoleName} to user {UserId}. Errors: {Errors}", 
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when trying to remove role {RoleName}", userId, roleName);
            return;
        }

        var hasRole = await _userManager.IsInRoleAsync(user, roleName);
        if (!hasRole)
        {
            _logger.LogInformation("User {UserId} does not have role {RoleName}", userId, roleName);
            return;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            _logger.LogInformation("Successfully removed role {RoleName} from user {UserId}", roleName, userId);
        }
        else
        {
            _logger.LogError("Failed to remove role {RoleName} from user {UserId}. Errors: {Errors}", 
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when trying to get roles", userId);
            return new List<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }
}
