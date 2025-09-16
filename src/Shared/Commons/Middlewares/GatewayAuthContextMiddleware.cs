using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ProductAuthMicroservice.Commons.Middlewares;

/// <summary>
/// Lightweight middleware for services behind API Gateway
/// Only extracts user context from headers set by gateway
/// </summary>
public class GatewayAuthContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayAuthContextMiddleware> _logger;

    public GatewayAuthContextMiddleware(RequestDelegate next, ILogger<GatewayAuthContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process if request comes from gateway (has auth method header)
        var authMethod = context.Request.Headers["X-Auth-Method"].FirstOrDefault();
        
        if (authMethod == "gateway-validated")
        {
            // Extract user information from headers set by gateway
            var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
            var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();
            var userRoles = context.Request.Headers["X-User-Roles"].FirstOrDefault();

            if (!string.IsNullOrEmpty(userId))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId)
                };

                if (!string.IsNullOrEmpty(userEmail))
                {
                    claims.Add(new Claim(ClaimTypes.Email, userEmail));
                }

                if (!string.IsNullOrEmpty(userRoles))
                {
                    var roles = userRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                    }
                }

                var identity = new ClaimsIdentity(claims, "gateway");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogDebug("User context set from gateway headers: {UserId}", userId);
            }
        }
        else
        {
            // For direct access (bypassing gateway), fall back to JWT validation
            // This provides flexibility for development or specific scenarios
            await ValidateDirectAccess(context);
        }

        await _next(context);
    }

    /// <summary>
    /// Handle direct access that bypasses gateway (optional fallback)
    /// </summary>
    private Task ValidateDirectAccess(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        bool requiresAuthorization = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null && 
                                   endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() == null;

        if (requiresAuthorization)
        {
            // Log warning for direct access to protected endpoints
            _logger.LogWarning("Direct access attempted to protected endpoint: {Path}. Consider routing through gateway.", 
                context.Request.Path);
            
            // You can either:
            // 1. Reject direct access: context.Response.StatusCode = 403; return;
            // 2. Allow but log: (current implementation)
            // 3. Perform full JWT validation: (implement if needed)
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for registering gateway auth context middleware
/// </summary>
public static class GatewayAuthContextMiddlewareExtensions
{
    /// <summary>
    /// Use gateway auth context middleware for services behind API Gateway
    /// </summary>
    public static IApplicationBuilder UseGatewayAuthContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GatewayAuthContextMiddleware>();
    }
}