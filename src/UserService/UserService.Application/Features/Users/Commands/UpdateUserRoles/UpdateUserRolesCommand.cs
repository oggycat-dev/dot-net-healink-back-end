using MediatR;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;

namespace UserService.Application.Features.Users.Commands.UpdateUserRoles;

/// <summary>
/// Command to update user roles (add/remove)
/// Updates AuthService and syncs to cache immediately
/// Uses RoleEnum for type safety
/// </summary>
public record UpdateUserRolesCommand : IRequest<Result>
{
    /// <summary>
    /// Auth Service User ID (AppUser.Id) - NOT UserProfile.Id
    /// Used for: Cache key, UserProfile query (FK), Event publishing
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Roles to add (enum values: 0=Admin, 1=Staff, 2=User, 3=ContentCreator)
    /// </summary>
    public List<RoleEnum> RolesToAdd { get; init; } = new();
    
    /// <summary>
    /// Roles to remove (enum values: 0=Admin, 1=Staff, 2=User, 3=ContentCreator)
    /// </summary>
    public List<RoleEnum> RolesToRemove { get; init; } = new();
}
