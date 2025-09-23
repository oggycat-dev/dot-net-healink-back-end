using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace UserService.Domain.Entities;

/// <summary>
/// Entity quản lý đơn đăng ký làm Content Creator
/// </summary>
public class CreatorApplication : BaseEntity
{
    /// <summary>
    /// ID của User nộp đơn
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Dữ liệu đơn đăng ký (JSON format)
    /// VD: {"portfolio": "...", "experience": "...", "socialMedia": "..."}
    /// </summary>
    public string ApplicationData { get; set; } = "{}";
    
    /// <summary>
    /// Trạng thái đơn đăng ký
    /// </summary>
    public ApplicationStatusEnum ApplicationStatus { get; set; } = ApplicationStatusEnum.Pending;
    
    /// <summary>
    /// Ngày nộp đơn
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Ngày được duyệt/từ chối
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
    
    /// <summary>
    /// Người duyệt đơn
    /// </summary>
    public Guid? ReviewedBy { get; set; }
    
    /// <summary>
    /// Lý do từ chối (nếu có)
    /// </summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// Ghi chú từ reviewer
    /// </summary>
    public string? ReviewNotes { get; set; }
    
    /// <summary>
    /// Business Role được yêu cầu (mặc định là ContentCreator)
    /// </summary>
    public Guid? RequestedBusinessRoleId { get; set; }
    
    // Navigation properties
    public virtual UserProfile User { get; set; } = null!;
    public virtual BusinessRole? RequestedBusinessRole { get; set; }
    public virtual UserProfile? ReviewedByUser { get; set; }
}
