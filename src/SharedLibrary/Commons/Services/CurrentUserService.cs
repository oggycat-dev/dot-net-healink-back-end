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

            // For distributed validation, we would need to call AuthService
            // But for now, if user is authenticated and has valid ID, consider valid
            // This can be enhanced to call AuthService validate endpoint
            
            return (true, userGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating current user");
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

            // Try to get roles from AuthService for up-to-date information
            try
            {
                var httpClient = _httpClientFactory.CreateClient("AuthService");
                
                // Add current authorization header if available
                var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", 
                            authHeader.Replace("Bearer ", ""));
                }

                var response = await httpClient.GetAsync($"/api/auth/user-roles/{userId.Value}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Models.Result<IList<string>>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (result?.IsSuccess == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get roles from AuthService, falling back to JWT claims");
            }

            // Fallback to JWT claims if AuthService call fails
            return Roles.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user roles");
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