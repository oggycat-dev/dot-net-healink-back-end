using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace UserService.Domain.Entities;

/// <summary>
/// Entity quản lý các vai trò nghiệp vụ (Content Creator, Community Moderator, etc.)
/// </summary>
public class BusinessRole : BaseEntity
{
    /// <summary>
    /// Tên vai trò (VD: ContentCreator, CommunityModerator)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên hiển thị (VD: "Content Creator", "Community Moderator")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả chi tiết về vai trò
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Enum type của business role
    /// </summary>
    public BusinessRoleEnum RoleType { get; set; }
    
    /// <summary>
    /// Role cơ bản cần có trong Auth Service (User, Staff, Admin)
    /// </summary>
    public RoleEnum RequiredCoreRole { get; set; } = RoleEnum.User;
    
    /// <summary>
    /// Có cần phê duyệt khi đăng ký vai trò này không
    /// </summary>
    public bool RequiresApproval { get; set; } = false;
    
    /// <summary>
    /// Danh sách permissions đặc biệt (JSON format)
    /// VD: ["content.podcast.create", "content.podcast.edit"]
    /// </summary>
    public string Permissions { get; set; } = "[]";
    
    /// <summary>
    /// Có đang hoạt động không
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Thứ tự ưu tiên (số càng nhỏ càng cao)
    /// </summary>
    public int Priority { get; set; } = 999;
    
    // Navigation properties
    public virtual ICollection<UserBusinessRole> UserBusinessRoles { get; set; } = new List<UserBusinessRole>();
}
