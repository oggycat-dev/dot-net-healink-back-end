using Microsoft.AspNetCore.Identity;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entity chính quản lý authentication trong Auth Service
/// </summary>
public class AppUser : IdentityUser<Guid>, IEntityLike
{
    /// <summary>
    /// Refresh token cho JWT
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Thời gian hết hạn refresh token
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityStatusEnum Status { get; set; }
    
    /// <summary>
    /// Lần cuối đăng nhập
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Lần cuối đăng xuất
    /// </summary>
    public DateTime? LastLogoutAt { get; set; }
    
}