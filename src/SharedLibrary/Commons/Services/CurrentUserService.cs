using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using SharedLibrary.Commons.Cache;

namespace SharedLibrary.Commons.Services;

/// <summary>
/// Implementation of current user service for distributed microservices
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IUserStateCache userStateCache,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value ??
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ??
        _httpContextAccessor.HttpContext?.User?.FindFirst("nameid")?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Get roles from Redis cache (real-time) if available, fallback to JWT claims
    /// </summary>
    public IEnumerable<string> Roles
    {
        get
        {
            // Try to get roles from Redis cache first (real-time roles)
            if (!string.IsNullOrEmpty(UserId) && Guid.TryParse(UserId, out var userGuid))
            {
                try
                {
                    var userState = _userStateCache.GetUserStateAsync(userGuid).GetAwaiter().GetResult();
                    if (userState != null && userState.Roles.Any())
                    {
                        _logger.LogDebug("Roles loaded from Redis cache for user {UserId}: {Roles}", 
                            userGuid, string.Join(", ", userState.Roles));
                        return userState.Roles;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get roles from Redis cache for user {UserId}, falling back to JWT claims", userGuid);
                }
            }

            // Fallback to JWT claims
            var claimRoles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? 
                           Enumerable.Empty<string>();
            
            _logger.LogDebug("Roles loaded from JWT claims for user {UserId}: {Roles}", 
                UserId ?? "unknown", string.Join(", ", claimRoles));
                
            return claimRoles;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

    public async Task<(bool isValid, Guid? userId)> IsUserValidAsync()
    {
        try
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(UserId))
            {
                return (false, null);
            }

            if (!Guid.TryParse(UserId, out var userGuid))
            {
                return (false, null);
            }

            // ✅ Check cache for user validity (NOT JWT)
            // Cache is source of truth for user status
            var userState = await _userStateCache.GetUserStateAsync(userGuid);
            
            if (userState == null)
            {
                _logger.LogWarning("User not found in cache - UserId: {UserId}", userGuid);
                return (false, null);
            }

            // User must be Active status
            if (userState.Status != Enums.EntityStatusEnum.Active)
            {
                _logger.LogWarning("User is not active in cache - UserId: {UserId}, Status: {Status}", 
                    userGuid, userState.Status);
                return (false, null);
            }

            _logger.LogDebug("User validated via cache - UserId: {UserId}, Status: Active", userGuid);
            return (true, userGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating current user via cache");
            return (false, null);
        }
    }

    public async Task<IList<string>> GetCurrentRolesAsync()
    {
        try
        {
            var (isValid, userId) = await IsUserValidAsync();
            if (!isValid || !userId.HasValue)
            {
                return new List<string>();
            }

            // ✅ Get roles from Redis cache ONLY (NOT JWT, NOT AuthService call)
            // Cache is single source of truth for roles
            var userState = await _userStateCache.GetUserStateAsync(userId.Value);
            
            if (userState == null || !userState.Roles.Any())
            {
                _logger.LogWarning("User state or roles not found in cache - UserId: {UserId}", userId.Value);
                return new List<string>();
            }

            _logger.LogDebug("Roles loaded from Redis cache for user {UserId}: {Roles}", 
                userId.Value, string.Join(", ", userState.Roles));
                
            return userState.Roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user roles from cache");
            return new List<string>();
        }
    }

    public async Task<(bool isValid, Guid? userId, IList<string> roles)> ValidateUserWithRolesAsync()
    {
        try
        {
            var (isValid, userId) = await IsUserValidAsync();
            if (!isValid || !userId.HasValue)
            {
                return (false, null, new List<string>());
            }

            var roles = await GetCurrentRolesAsync();
            return (true, userId, roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user with roles");
            return (false, null, new List<string>());
        }
    }

    public async Task<bool> HasRoleAsync(string role)
    {
        try
        {
            var roles = await GetCurrentRolesAsync();
            return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role: {Role}", role);
            return false;
        }
    }

    public async Task<bool> HasAnyRoleAsync(params string[] roles)
    {
        try
        {
            var userRoles = await GetCurrentRolesAsync();
            return roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user roles: {Roles}", string.Join(", ", roles));
            return false;
        }
    }

    public async Task<bool> HasAllRolesAsync(params string[] roles)
    {
        try
        {
            var userRoles = await GetCurrentRolesAsync();
            return roles.All(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user roles: {Roles}", string.Join(", ", roles));
            return false;
        }
    }
}