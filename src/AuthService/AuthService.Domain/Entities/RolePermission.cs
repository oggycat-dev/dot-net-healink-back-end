using SharedLibrary.Commons.Entities;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entity liên kết giữa Role và Permission (Many-to-Many)
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// ID của Role
    /// </summary>
    public Guid RoleId { get; set; }
    
    /// <summary>
    /// ID của Permission
    /// </summary>
    public Guid PermissionId { get; set; }
    
    /// <summary>
    /// Ngày gán quyền
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Người gán quyền
    /// </summary>
    public Guid? AssignedBy { get; set; }
    
    // Navigation properties
    public virtual AppRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
