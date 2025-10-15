namespace SharedLibrary.Commons.Enums;

/// <summary>
/// Trạng thái đơn đăng ký làm Content Creator
/// </summary>
public enum ApplicationStatusEnum
{
    Pending = 0,    // Chờ duyệt
    Approved = 1,   // Đã duyệt
    Rejected = 2,   // Bị từ chối
    Withdrawn = 3   // Rút lại
}
