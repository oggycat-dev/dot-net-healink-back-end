using Microsoft.Extensions.Options;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Configs;
using ProductAuthMicroservice.Commons.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace ProductAuthMicroservice.Gateway.API.Middlewares;

/// <summary>
/// Middleware để validate user state từ cache thay vì chỉ dựa vào JWT
/// </summary>
public class DistributedAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DistributedAuthMiddleware> _logger;
    private readonly IUserStateCache _userStateCache;
    private readonly IJwtService _jwtService;
    private readonly JwtConfig _jwtConfig;

    public DistributedAuthMiddleware(
        RequestDelegate next,
        ILogger<DistributedAuthMiddleware> logger,
        IUserStateCache userStateCache,
        IJwtService jwtService,
        IOptions<JwtConfig> jwtConfig)
    {
        _next = next;
        _logger = logger;
        _userStateCache = userStateCache;
        _jwtService = jwtService;
        _jwtConfig = jwtConfig.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health endpoints và public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var token = ExtractTokenFromHeader(context.Request);
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            // 1. Validate JWT structure và signature
            if (!_jwtService.ValidateToken(token))
            {
                _logger.LogWarning("Invalid JWT token received");
                await HandleUnauthorized(context, "Invalid token");
                return;
            }

            // 2. Extract user ID từ JWT
            var userId = _jwtService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID in token");
                await HandleUnauthorized(context, "Invalid user ID");
                return;
            }

            // 3. Check user state từ cache (CRITICAL CHECK)
            var userState = await _userStateCache.GetUserStateAsync(userGuid);
            if (userState == null)
            {
                _logger.LogWarning("User {UserId} not found in cache - token invalid", userGuid);
                await HandleUnauthorized(context, "User session not found");
                return;
            }

            // 4. Check user active status
            if (!userState.IsActive)
            {
                _logger.LogWarning("User {UserId} is inactive - status: {Status}", userGuid, userState.Status);
                await HandleUnauthorized(context, "User account is inactive");
                return;
            }

            // 5. Validate refresh token (nếu có trong request)
            var refreshToken = ExtractRefreshTokenFromHeader(context.Request);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var isRefreshTokenValid = await _userStateCache.IsRefreshTokenValidAsync(userGuid, refreshToken);
                if (!isRefreshTokenValid)
                {
                    _logger.LogWarning("Invalid refresh token for user {UserId}", userGuid);
                    await HandleUnauthorized(context, "Invalid refresh token");
                    return;
                }
            }

            // 6. Set user claims từ cache (not from JWT)
            await SetUserClaimsFromCache(context, userState);

            _logger.LogDebug("User {UserId} authenticated successfully via cache", userGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in distributed auth middleware");
            await HandleInternalError(context, "Authentication error");
            return;
        }

        await _next(context);
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private static string? ExtractRefreshTokenFromHeader(HttpRequest request)
    {
        return request.Headers["X-Refresh-Token"].FirstOrDefault();
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh-token",
            "/swagger",
            "/api-docs"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SetUserClaimsFromCache(HttpContext context, UserStateInfo userState)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userState.UserId.ToString()),
            new(ClaimTypes.Email, userState.Email),
            new("user_status", userState.Status.ToString()),
            new("cache_updated_at", userState.CacheUpdatedAt.ToString("O"))
        };

        // Add role claims từ cache
        foreach (var role in userState.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "DistributedAuth");
        context.User = new ClaimsPrincipal(identity);

        // Set additional context data
        context.Items["UserId"] = userState.UserId;
        context.Items["UserRoles"] = userState.Roles;
        context.Items["UserStatus"] = userState.Status;
    }

    private async Task HandleUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Unauthorized",
            message = message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleInternalError(HttpContext context, string message)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Internal Server Error",
            message = message,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
