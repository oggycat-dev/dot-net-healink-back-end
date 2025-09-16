using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Attributes;

/// <summary>
/// Attribute to enforce authorization on a CQRS request
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Comma-separated list of roles
    /// </summary>
    public string Roles { get; set; } = string.Empty;
}

/// <summary>
/// MVC Authorization attribute with role-based access control
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRolesAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    
    public AuthorizeRolesAttribute(params string[] roles)
    {
        _roles = roles;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            
        if (allowAnonymous)
            return;
            
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(
                Result.Failure("You are not authenticated.", ErrorCodeEnum.Unauthorized));
            return;
        }
        
        if (_roles.Length == 0)
            return;
            
        var userRoleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role);
        if (userRoleClaim == null)
        {
            context.Result = new ObjectResult(
                Result.Failure("You do not have the required role to access this resource.", ErrorCodeEnum.Forbidden))
            {
                StatusCode = 403
            };
            return;
        }
        
        var userRole = userRoleClaim.Value;
        
        if (!_roles.Contains(userRole))
        {
            context.Result = new ObjectResult(
                Result.Failure($"This resource requires one of these roles: {string.Join(", ", _roles)}. Your role: {userRole}", ErrorCodeEnum.InsufficientPermissions))
            {
                StatusCode = 403
            };
            return;
        }
    }
} 