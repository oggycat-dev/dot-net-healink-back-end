using SharedLibrary.Commons.Entities;

namespace ContentService.Domain.Entities;

/// <summary>
/// Lưu trữ cài đặt cho Content Creator
/// </summary>
public class CreatorSettings : BaseEntity
{
    /// <summary>
    /// User ID của creator
    /// </summary>
    public Guid CreatorId { get; set; }
    
    /// <summary>
    /// Tên hiển thị của creator
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Bio ngắn của creator
    /// </summary>
    public string? Bio { get; set; }
    
    /// <summary>
    /// Ảnh đại diện của creator
    /// </summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Giới hạn số lượng nội dung có thể đăng
    /// </summary>
    public int MaxContentQuota { get; set; } = 50;
    
    /// <summary>
    /// Cho phép tự động publish không cần duyệt
    /// </summary>
    public bool AutoPublish { get; set; } = false;
    
    /// <summary>
    /// Thiết lập tùy chỉnh khác (JSON)
    /// </summary>
    public string? CustomSettings { get; set; }
    
    // Navigation properties cho Content
    public virtual ICollection<Content> Contents { get; set; } = new List<Content>();
}
