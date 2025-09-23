using Microsoft.AspNetCore.Identity;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entity quản lý các role cơ bản trong Auth Service
/// </summary>
public class AppRole : IdentityRole<Guid>, IEntityLike
{
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
