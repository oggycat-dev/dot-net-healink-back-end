namespace AuthService.Application.Commons.Interfaces;

/// <summary>
/// Service for managing user roles
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Add role to user in database
    /// </summary>
    Task AddRoleToUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove role from user in database
    /// </summary>
    Task RemoveRoleFromUserAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all roles for user from database
    /// </summary>
    Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}
