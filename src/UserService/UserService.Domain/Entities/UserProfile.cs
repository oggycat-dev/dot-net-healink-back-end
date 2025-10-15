using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace UserService.Domain.Entities;

/// <summary>
/// Entity quản lý thông tin profile chi tiết của người dùng
/// </summary>
public class UserProfile : BaseEntity
{
    /// <summary>
    /// User ID từ Auth Service (Foreign Key)
    /// Nullable to allow pre-creation of UserProfile before AuthUser exists (Saga pattern)
    /// Will be set to actual UserId when AuthUser is created
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// Họ tên đầy đủ
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email (sync từ Auth Service)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Đường dẫn avatar
    /// </summary>
    public string? AvatarPath { get; set; }

    /// <summary>
    /// Lần cuối đăng nhập (sync từ Auth Service)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserBusinessRole> UserBusinessRoles { get; set; } = new List<UserBusinessRole>();
    public virtual ICollection<CreatorApplication> CreatorApplications { get; set; } = new List<CreatorApplication>();
    public virtual ICollection<CreatorApplication> ReviewedApplications { get; set; } = new List<CreatorApplication>();
    public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
}