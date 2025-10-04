using MediatR;

namespace UserService.Application.Features.CreatorApplications.Queries;

/// <summary>
/// Query để lấy trạng thái đơn đăng ký Content Creator của user hiện tại
/// </summary>
public class GetMyApplicationStatusQuery : IRequest<MyApplicationStatusDto?>
{
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO cho trạng thái đơn đăng ký Content Creator của user hiện tại
/// </summary>
public class MyApplicationStatusDto
{
    /// <summary>
    /// ID của đơn đăng ký
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Trạng thái đơn đăng ký (Pending, Approved, Rejected)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả trạng thái bằng tiếng Việt
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian nộp đơn
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Thời gian duyệt đơn
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// ID của người duyệt đơn
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// Lý do từ chối (nếu có)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Kinh nghiệm làm việc
    /// </summary>
    public string Experience { get; set; } = string.Empty;

    /// <summary>
    /// Portfolio/CV
    /// </summary>
    public string Portfolio { get; set; } = string.Empty;

    /// <summary>
    /// Động lực làm Content Creator
    /// </summary>
    public string Motivation { get; set; } = string.Empty;

    /// <summary>
    /// Thông tin mạng xã hội
    /// </summary>
    public List<string> SocialMedia { get; set; } = new();

    /// <summary>
    /// Thông tin bổ sung
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Vai trò kinh doanh được yêu cầu
    /// </summary>
    public string RequestedBusinessRole { get; set; } = string.Empty;

    /// <summary>
    /// Có thể nộp đơn lại không
    /// </summary>
    public bool CanResubmit { get; set; }

    /// <summary>
    /// Bước tiếp theo cần làm
    /// </summary>
    public string NextSteps { get; set; } = string.Empty;
}