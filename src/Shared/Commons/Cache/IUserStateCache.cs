using ProductAuthMicroservice.Commons.Enums;

namespace ProductAuthMicroservice.Commons.Cache;

/// <summary>
/// Cache service để quản lý trạng thái user trong distributed system
/// </summary>
public interface IUserStateCache
{
    /// <summary>
    /// Lưu trạng thái user vào cache
    /// </summary>
    Task SetUserStateAsync(UserStateInfo userState, TimeSpan? expiration = null);

    /// <summary>
    /// Lấy trạng thái user từ cache
    /// </summary>
    Task<UserStateInfo?> GetUserStateAsync(Guid userId);

    /// <summary>
    /// Xóa trạng thái user khỏi cache
    /// </summary>
    Task RemoveUserStateAsync(Guid userId);

    /// <summary>
    /// Check user có active và có quyền không
    /// </summary>
    Task<bool> IsUserActiveAsync(Guid userId);

    /// <summary>
    /// Check user có role cụ thể không
    /// </summary>
    Task<bool> HasRoleAsync(Guid userId, string role);

    /// <summary>
    /// Check refresh token có valid không
    /// </summary>
    Task<bool> IsRefreshTokenValidAsync(Guid userId, string refreshToken);

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    Task RevokeRefreshTokenAsync(Guid userId);

    /// <summary>
    /// Update user roles trong cache
    /// </summary>
    Task UpdateUserRolesAsync(Guid userId, List<string> roles);

    /// <summary>
    /// Update user status trong cache
    /// </summary>
    Task UpdateUserStatusAsync(Guid userId, EntityStatusEnum status);

    /// <summary>
    /// Lấy tất cả active users (cho monitoring)
    /// </summary>
    Task<List<UserStateInfo>> GetActiveUsersAsync();

    /// <summary>
    /// Cleanup expired tokens
    /// </summary>
    Task CleanupExpiredTokensAsync();
}

/// <summary>
/// Thông tin trạng thái user trong cache
/// </summary>
public record UserStateInfo
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public EntityStatusEnum Status { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? RefreshTokenExpiryTime { get; init; }
    public DateTime LastLoginAt { get; init; }
    public DateTime CacheUpdatedAt { get; init; } = DateTime.UtcNow;
    
    public bool IsActive => Status == EntityStatusEnum.Active;
    public bool IsRefreshTokenValid => 
        !string.IsNullOrEmpty(RefreshToken) && 
        RefreshTokenExpiryTime.HasValue && 
        RefreshTokenExpiryTime.Value > DateTime.UtcNow;
}
