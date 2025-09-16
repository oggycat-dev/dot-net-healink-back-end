using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.Enums;
using System.Text.Json;

namespace ProductAuthMicroservice.Commons.Cache;

public class RedisUserStateCache : IUserStateCache
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisUserStateCache> _logger;
    private readonly RedisConfig _config;
    private const string USER_STATE_PREFIX = "user_state:";
    private const string ACTIVE_USERS_KEY = "active_users";

    public RedisUserStateCache(
        IDistributedCache distributedCache,
        ILogger<RedisUserStateCache> logger,
        IOptions<RedisConfig> config)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _config = config.Value;
    }

    public async Task SetUserStateAsync(UserStateInfo userState, TimeSpan? expiration = null)
    {
        try
        {
            var key = GetUserStateKey(userState.UserId);
            var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_config.UserStateCacheMinutes);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(_config.UserStateSlidingMinutes)
            };

            var jsonData = JsonSerializer.Serialize(userState);
            await _distributedCache.SetStringAsync(key, jsonData, options);
            
            // Update active users list
            await UpdateActiveUsersListAsync(userState);
            
            _logger.LogDebug("User state cached in Redis for user {UserId} with expiration {Expiration}", 
                userState.UserId, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching user state in Redis for user {UserId}", userState.UserId);
            throw;
        }
    }

    public async Task<UserStateInfo?> GetUserStateAsync(Guid userId)
    {
        try
        {
            var key = GetUserStateKey(userId);
            var jsonData = await _distributedCache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                var userState = JsonSerializer.Deserialize<UserStateInfo>(jsonData);
                _logger.LogDebug("User state found in Redis for user {UserId}", userId);
                return userState;
            }

            _logger.LogDebug("User state not found in Redis for user {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user state from Redis for user {UserId}", userId);
            return null;
        }
    }

    public async Task RemoveUserStateAsync(Guid userId)
    {
        try
        {
            var key = GetUserStateKey(userId);
            await _distributedCache.RemoveAsync(key);
            
            // Remove from active users list
            await RemoveFromActiveUsersListAsync(userId);
            
            _logger.LogDebug("User state removed from Redis for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user state from Redis for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsUserActiveAsync(Guid userId)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState == null)
            {
                _logger.LogWarning("User {UserId} not found in Redis cache - considered inactive", userId);
                return false;
            }

            var isActive = userState.IsActive;
            _logger.LogDebug("User {UserId} active status: {IsActive}", userId, isActive);
            return isActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user active status for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(Guid userId, string role)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState == null || !userState.IsActive)
            {
                return false;
            }

            var hasRole = userState.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("User {UserId} has role {Role}: {HasRole}", userId, role, hasRole);
            return hasRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role for user {UserId}, role {Role}", userId, role);
            return false;
        }
    }

    public async Task<bool> IsRefreshTokenValidAsync(Guid userId, string refreshToken)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState == null || !userState.IsActive)
            {
                return false;
            }

            var isValid = userState.RefreshToken == refreshToken && userState.IsRefreshTokenValid;
            _logger.LogDebug("Refresh token validity for user {UserId}: {IsValid}", userId, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return false;
        }
    }

    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState != null)
            {
                var updatedState = userState with 
                { 
                    RefreshToken = null, 
                    RefreshTokenExpiryTime = null,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                
                await SetUserStateAsync(updatedState);
                _logger.LogDebug("Refresh token revoked for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateUserRolesAsync(Guid userId, List<string> roles)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState != null)
            {
                var updatedState = userState with 
                { 
                    Roles = roles,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                
                await SetUserStateAsync(updatedState);
                _logger.LogDebug("Roles updated for user {UserId}: {Roles}", userId, string.Join(", ", roles));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateUserStatusAsync(Guid userId, EntityStatusEnum status)
    {
        try
        {
            var userState = await GetUserStateAsync(userId);
            if (userState != null)
            {
                var updatedState = userState with 
                { 
                    Status = status,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                
                await SetUserStateAsync(updatedState);
                _logger.LogDebug("Status updated for user {UserId}: {Status}", userId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserStateInfo>> GetActiveUsersAsync()
    {
        try
        {
            var jsonData = await _distributedCache.GetStringAsync(ACTIVE_USERS_KEY);
            if (!string.IsNullOrEmpty(jsonData))
            {
                var activeUserIds = JsonSerializer.Deserialize<List<Guid>>(jsonData) ?? new List<Guid>();
                var activeUsers = new List<UserStateInfo>();
                
                foreach (var userId in activeUserIds)
                {
                    var userState = await GetUserStateAsync(userId);
                    if (userState != null && userState.IsActive)
                    {
                        activeUsers.Add(userState);
                    }
                }
                
                return activeUsers;
            }
            
            return new List<UserStateInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active users from Redis");
            return new List<UserStateInfo>();
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            var activeUsers = await GetActiveUsersAsync();
            var expiredUsers = activeUsers.Where(u => !u.IsRefreshTokenValid).ToList();
            
            foreach (var expiredUser in expiredUsers)
            {
                await RevokeRefreshTokenAsync(expiredUser.UserId);
                _logger.LogDebug("Cleaned up expired token for user {UserId}", expiredUser.UserId);
            }
            
            _logger.LogInformation("Cleaned up {Count} expired tokens", expiredUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of expired tokens");
        }
    }

    private string GetUserStateKey(Guid userId) => $"{USER_STATE_PREFIX}{userId}";

    private async Task UpdateActiveUsersListAsync(UserStateInfo userState)
    {
        try
        {
            var jsonData = await _distributedCache.GetStringAsync(ACTIVE_USERS_KEY);
            var activeUsers = string.IsNullOrEmpty(jsonData) 
                ? new List<Guid>() 
                : JsonSerializer.Deserialize<List<Guid>>(jsonData) ?? new List<Guid>();
            
            if (userState.IsActive && !activeUsers.Contains(userState.UserId))
            {
                activeUsers.Add(userState.UserId);
            }
            else if (!userState.IsActive && activeUsers.Contains(userState.UserId))
            {
                activeUsers.Remove(userState.UserId);
            }
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_config.ActiveUsersListHours)
            };

            var updatedJsonData = JsonSerializer.Serialize(activeUsers);
            await _distributedCache.SetStringAsync(ACTIVE_USERS_KEY, updatedJsonData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating active users list in Redis");
        }
    }

    private async Task RemoveFromActiveUsersListAsync(Guid userId)
    {
        try
        {
            var jsonData = await _distributedCache.GetStringAsync(ACTIVE_USERS_KEY);
            if (!string.IsNullOrEmpty(jsonData))
            {
                var activeUsers = JsonSerializer.Deserialize<List<Guid>>(jsonData) ?? new List<Guid>();
                if (activeUsers.Remove(userId))
                {
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_config.ActiveUsersListHours)
                    };

                    var updatedJsonData = JsonSerializer.Serialize(activeUsers);
                    await _distributedCache.SetStringAsync(ACTIVE_USERS_KEY, updatedJsonData, options);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from active users list in Redis");
        }
    }
}