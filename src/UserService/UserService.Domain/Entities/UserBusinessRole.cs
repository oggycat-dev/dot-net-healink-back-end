using SharedLibrary.Commons.Entities;

namespace UserService.Domain.Entities;

/// <summary>
/// Entity liên kết giữa User và Business Role (Many-to-Many)
/// </summary>
public class UserBusinessRole : BaseEntity
{
    /// <summary>
    /// ID của User (từ UserProfile)
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// ID của Business Role
    /// </summary>
    public Guid BusinessRoleId { get; set; }
    
    /// <summary>
    /// Ngày được gán vai trò
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Người gán vai trò (Admin hoặc system)
    /// </summary>
    public Guid? AssignedBy { get; set; }
    
    /// <summary>
    /// Ngày hết hạn vai trò (null = vĩnh viễn)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>
    /// Ghi chú thêm
    /// </summary>
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual BusinessRole BusinessRole { get; set; } = null!;
}
