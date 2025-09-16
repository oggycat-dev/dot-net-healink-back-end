using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Services;
using System.Text.Json;

namespace ProductAuthMicroservice.Commons.Filters;

/// <summary>
/// Filter to validate user roles from database for distributed authorization
/// </summary>
public class RoleAccessFilter : IAsyncAuthorizationFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RoleAccessFilter> _logger;
    private readonly string[] _requiredRoles;

    public RoleAccessFilter(
        ICurrentUserService currentUserService,
        ILogger<RoleAccessFilter> logger,
        params string[] requiredRoles)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _requiredRoles = requiredRoles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            // Skip if action allows anonymous access
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
            {
                return;
            }

            // Check if user is authenticated
            if (!_currentUserService.IsAuthenticated)
            {
                await SetUnauthorizedResult(context, "User is not authenticated", ErrorCodeEnum.Unauthorized);
                return;
            }

            // Validate user and get roles from database
            var (isValid, userId, roles) = await _currentUserService.ValidateUserWithRolesAsync();

            if (!isValid || !userId.HasValue)
            {
                await SetUnauthorizedResult(context, "User validation failed", ErrorCodeEnum.InvalidToken);
                return;
            }

            // Check if user has any required roles
            if (_requiredRoles.Length > 0)
            {
                var hasRequiredRole = _requiredRoles.Any(role => 
                    roles.Contains(role, StringComparer.OrdinalIgnoreCase));

                if (!hasRequiredRole)
                {
                    await SetForbiddenResult(context, 
                        $"Access denied. Required roles: {string.Join(", ", _requiredRoles)}", 
                        ErrorCodeEnum.Forbidden);
                    return;
                }
            }

            _logger.LogDebug("Role access validation successful for user {UserId} with roles {Roles}", 
                userId, string.Join(", ", roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in role access filter");
            await SetUnauthorizedResult(context, "Authorization validation failed", ErrorCodeEnum.InternalError);
        }
    }

    private static async Task SetUnauthorizedResult(AuthorizationFilterContext context, string message, ErrorCodeEnum errorCode)
    {
        var result = Result.Failure(message, errorCode);
        
        context.Result = new UnauthorizedObjectResult(result);
        context.HttpContext.Response.ContentType = "application/json";
        
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static async Task SetForbiddenResult(AuthorizationFilterContext context, string message, ErrorCodeEnum errorCode)
    {
        var result = Result.Failure(message, errorCode);
        
        context.Result = new ObjectResult(result) { StatusCode = 403 };
        context.HttpContext.Response.ContentType = "application/json";
        
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

// TODO: Implement RequireRolesAttribute later if needed
