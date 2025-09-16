using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Services;
using System.Security.Claims;

namespace ProductAuthMicroservice.Commons.Attributes;

/// <summary>
/// Authorization attribute sử dụng distributed cache để check user state
/// Thay thế cho [Authorize] attribute để có kiểm soát chặt chẽ hơn
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class DistributedAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string[] Roles { get; set; } = Array.Empty<string>();
    public EntityStatusEnum RequiredStatus { get; set; } = EntityStatusEnum.Active;
    public bool AllowAnonymous { get; set; } = false;

    public DistributedAuthorizeAttribute()
    {
    }

    public DistributedAuthorizeAttribute(params string[] roles)
    {
        Roles = roles ?? Array.Empty<string>();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if action has AllowAnonymous
        if (AllowAnonymous || context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return;
        }

        var userStateCache = context.HttpContext.RequestServices.GetService<IUserStateCache>();
        var currentUserService = context.HttpContext.RequestServices.GetService<ICurrentUserService>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<DistributedAuthorizeAttribute>>();

        if (userStateCache == null || currentUserService == null)
        {
            logger?.LogError("Required services not found for distributed authorization");
            context.Result = new StatusCodeResult(500);
            return;
        }

        try
        {
            // 1. Check if user is authenticated
            var userIdString = currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                logger?.LogWarning("User not authenticated or invalid user ID");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "Authentication required"
                });
                return;
            }

            // 2. Get user state từ cache (CRITICAL CHECK)
            var userState = await userStateCache.GetUserStateAsync(userId);
            if (userState == null)
            {
                logger?.LogWarning("User {UserId} not found in cache", userId);
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "User session not found"
                });
                return;
            }

            // 3. Check user status
            if (userState.Status != RequiredStatus)
            {
                logger?.LogWarning("User {UserId} status {Status} does not meet requirement {RequiredStatus}", 
                    userId, userState.Status, RequiredStatus);
                context.Result = new ForbidResult();
                return;
            }

            // 4. Check if user is active
            if (!userState.IsActive)
            {
                logger?.LogWarning("User {UserId} is not active - status: {Status}", userId, userState.Status);
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "User account is inactive"
                });
                return;
            }

            // 5. Check roles if specified
            if (Roles.Length > 0)
            {
                var hasRequiredRole = false;
                foreach (var role in Roles)
                {
                    if (await userStateCache.HasRoleAsync(userId, role))
                    {
                        hasRequiredRole = true;
                        break;
                    }
                }

                if (!hasRequiredRole)
                {
                    logger?.LogWarning("User {UserId} does not have required roles: {Roles}", 
                        userId, string.Join(", ", Roles));
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // 6. Update HttpContext với user state mới nhất
            await UpdateHttpContextWithUserState(context.HttpContext, userState);

            logger?.LogDebug("User {UserId} authorized successfully via distributed cache", userId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in distributed authorization for user {UserId}", currentUserService?.UserId);
            context.Result = new StatusCodeResult(500);
        }
    }

    private async Task UpdateHttpContextWithUserState(HttpContext httpContext, UserStateInfo userState)
    {
        // Update claims với state mới nhất từ cache
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
        httpContext.User = new ClaimsPrincipal(identity);

        // Set context items
        httpContext.Items["UserId"] = userState.UserId;
        httpContext.Items["UserRoles"] = userState.Roles;
        httpContext.Items["UserStatus"] = userState.Status;
        httpContext.Items["UserState"] = userState;
    }
}

/// <summary>
/// Specialized attribute for role-based authorization
/// </summary>
public class DistributedAuthorizeRolesAttribute : DistributedAuthorizeAttribute
{
    public DistributedAuthorizeRolesAttribute(params string[] roles) : base(roles)
    {
    }
}

/// <summary>
/// Specialized attribute for admin-only access
/// </summary>
public class DistributedAuthorizeAdminAttribute : DistributedAuthorizeAttribute
{
    public DistributedAuthorizeAdminAttribute() : base("Admin", "SuperAdmin")
    {
    }
}

/// <summary>
/// Specialized attribute for manager+ access
/// </summary>
public class DistributedAuthorizeManagerAttribute : DistributedAuthorizeAttribute
{
    public DistributedAuthorizeManagerAttribute() : base("Manager", "Admin", "SuperAdmin")
    {
    }
}
