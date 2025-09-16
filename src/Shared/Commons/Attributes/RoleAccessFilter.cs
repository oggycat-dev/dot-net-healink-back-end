using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Attributes;

/// <summary>
/// Base filter for system access control across microservices
/// </summary>
public abstract class SystemAccessFilterBase : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Method == "OPTIONS")
            return;
            
        if (context.HttpContext.Request.Method == "POST" && 
            context.HttpContext.Request.HasJsonContentType())
        {
            context.HttpContext.Items["ProcessLoginResult"] = true;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items["ProcessLoginResult"] == null)
            return;
            
        if (context.Result is not ObjectResult objectResult || objectResult.StatusCode != 200)
            return;
        
        // This would need to be adapted based on your specific auth response structure
        // For microservices, this might need to check different response types
        if (objectResult.Value is not Result<IAuthResponse> authResult || !authResult.IsSuccess)
            return;
            
        var authResponse = authResult.Data;
        
        if (authResponse != null && !IsAuthorizedForSystem(authResponse))
        {
            var systemName = GetSystemName();
            var allowedRoles = GetAllowedRolesDescription();
            
            context.Result = new ObjectResult(Result.Failure(
                $"You do not have access to the {systemName}.",
                ErrorCodeEnum.InsufficientPermissions,
                new List<string> { $"This area is only accessible to {allowedRoles}. Please use an appropriate account." }))
            {
                StatusCode = 403
            };
        }
    }
    
    protected abstract bool IsAuthorizedForSystem(IAuthResponse user);
    protected abstract string GetSystemName();
    protected abstract string GetAllowedRolesDescription();
    
    /// <summary>
    /// Check if the user has a specific role based on the roles list
    /// </summary>
    protected bool HasRole(IAuthResponse user, string role)
    {
        return user.Roles?.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;
    }
}

/// <summary>
/// Interface for auth response to support different microservice implementations
/// </summary>
public interface IAuthResponse
{
    string[] Roles { get; }
    string UserId { get; }
    string UserName { get; }
}

/// <summary>
/// Filter that allows only customer access
/// </summary>
public class CustomerRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(IAuthResponse user)
    {
        return HasRole(user, "Customer") || HasRole(user, "Staff") || HasRole(user, "Admin");
    }
    
    protected override string GetSystemName()
    {
        return "Customer Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Customers, Staff members, and Administrators";
    }
}

/// <summary>
/// Filter that allows only staff and admin access
/// </summary>
public class StaffRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(IAuthResponse user)
    {
        return HasRole(user, "Staff") || HasRole(user, "Admin");
    }
    
    protected override string GetSystemName()
    {
        return "Staff Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Staff members and Administrators";
    }
}

/// <summary>
/// Filter that allows only admin access
/// </summary>
public class AdminRoleAccessFilter : SystemAccessFilterBase
{
    protected override bool IsAuthorizedForSystem(IAuthResponse user)
    {
        return HasRole(user, "Admin");
    }
    
    protected override string GetSystemName()
    {
        return "Admin Portal";
    }
    
    protected override string GetAllowedRolesDescription()
    {
        return "Administrators only";
    }
}
