using SharedLibrary.Commons.Enums;

namespace UserService.Application.Commons.DTOs;

/// <summary>
/// Request DTO for updating user status
/// Updates both AuthService and UserService, syncs to cache immediately
/// </summary>
public class UpdateUserStatusRequest
{
    /// <summary>
    /// New status to set
    /// 0=Active, 1=Inactive, 2=Deleted, 3=Pending
    /// </summary>
    public EntityStatusEnum Status { get; set; }
    
    /// <summary>
    /// Reason for status change (for audit)
    /// </summary>
    public string? Reason { get; set; }
}
