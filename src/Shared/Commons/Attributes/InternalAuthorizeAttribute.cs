using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Attributes;

/// <summary>
/// Simple authorization attribute for internal services that trust Gateway's authentication
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class InternalAuthorizeAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    
    public InternalAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check for AllowAnonymous
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            
        if (allowAnonymous)
            return;
            
        // Check if user is authenticated (Gateway should have set this)
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(
                Result.Failure("You are not authenticated.", ErrorCodeEnum.Unauthorized));
            return;
        }
        
        // If no specific roles required, just authentication is enough
        if (_roles.Length == 0)
            return;
            
        // Check roles (Gateway should have set these claims)
        var userRoles = context.HttpContext.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
            
        if (!userRoles.Any())
        {
            context.Result = new ObjectResult(
                Result.Failure("You do not have any roles assigned.", ErrorCodeEnum.Forbidden))
            {
                StatusCode = 403
            };
            return;
        }
        
        // Check if user has any of the required roles
        var hasRequiredRole = _roles.Any(requiredRole => 
            userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));
            
        if (!hasRequiredRole)
        {
            context.Result = new ObjectResult(
                Result.Failure($"This resource requires one of these roles: {string.Join(", ", _roles)}. Your roles: {string.Join(", ", userRoles)}", ErrorCodeEnum.InsufficientPermissions))
            {
                StatusCode = 403
            };
            return;
        }
    }
}

/// <summary>
/// Simple authorize attribute without role requirements for internal services
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InternalAuthorizeSimpleAttribute : InternalAuthorizeAttribute
{
    public InternalAuthorizeSimpleAttribute() : base()
    {
    }
}