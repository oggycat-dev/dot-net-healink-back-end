using SharedLibrary.Commons.Enums;

namespace UserService.Application.Commons.DTOs;

/// <summary>
/// Response DTO for user profile with roles from AuthService
/// </summary>
public class UserProfileResponse
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// UserId from AuthService - nullable during pending state
    /// </summary>
    public Guid? UserId { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public EntityStatusEnum Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Roles fetched from AuthService via RPC
    /// </summary>
    public List<string> Roles { get; set; } = new List<string>();
}
