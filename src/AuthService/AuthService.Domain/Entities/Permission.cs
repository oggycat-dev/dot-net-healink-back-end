using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entity quản lý các quyền hạn cơ bản trong hệ thống
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// Tên quyền (VD: content.create, user.manage)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên hiển thị (VD: "Create Content", "Manage Users")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả chi tiết về quyền
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Module mà quyền này thuộc về
    /// </summary>
    public PermissionModuleEnum Module { get; set; }
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
