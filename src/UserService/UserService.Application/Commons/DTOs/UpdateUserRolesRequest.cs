using SharedLibrary.Commons.Enums;

namespace UserService.Application.Commons.DTOs;

/// <summary>
/// Request DTO for updating user roles
/// Supports adding and removing roles in single operation
/// Uses RoleEnum for type safety
/// </summary>
public class UpdateUserRolesRequest
{
    /// <summary>
    /// Roles to add to user
    /// Example: [RoleEnum.ContentCreator, RoleEnum.Staff]
    /// Values: 0=Admin, 1=Staff, 2=User, 3=ContentCreator
    /// </summary>
    public List<RoleEnum> RolesToAdd { get; set; } = new();
    
    /// <summary>
    /// Roles to remove from user
    /// Example: [RoleEnum.User]
    /// Values: 0=Admin, 1=Staff, 2=User, 3=ContentCreator
    /// </summary>
    public List<RoleEnum> RolesToRemove { get; set; } = new();
}
