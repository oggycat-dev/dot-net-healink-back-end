using SharedLibrary.Commons.Entities;

namespace UserService.Domain.Entities;

/// <summary>
/// Entity ghi lại các hoạt động của người dùng để audit
/// </summary>
public class UserActivityLog : BaseEntity
{
    /// <summary>
    /// ID của User thực hiện hoạt động
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Loại hoạt động (VD: "LoginSuccess", "ProfileUpdated", "RoleAssigned")
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả chi tiết hoạt động
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Metadata bổ sung (JSON format)
    /// </summary>
    public string Metadata { get; set; } = "{}";
    
    /// <summary>
    /// IP Address của user
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User Agent
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Thời gian thực hiện hoạt động
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
}
