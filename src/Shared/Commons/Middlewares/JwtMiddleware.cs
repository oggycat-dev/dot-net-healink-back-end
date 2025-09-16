using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.Commons.Services;

namespace ProductAuthMicroservice.Commons.Middlewares;

/// <summary>
/// Middleware for validating JWT tokens in requests across microservices
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    
    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    /// <summary>
    /// Process the request to validate JWT token
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var endpoint = context.GetEndpoint();
        bool requiresAuthorization = false;
        
        if (endpoint != null)
        {
            requiresAuthorization = endpoint.Metadata.GetMetadata<AuthorizeAttribute>() != null && 
                                  endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() == null;
        }
        
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        
        if (token != null)
        {
            var userAttached = AttachUserToContext(context, jwtService, token);
            
            if (!userAttached && requiresAuthorization)
            {
                await SendUnauthorizedResponse(context, "Invalid or expired token", ErrorCodeEnum.InvalidToken);
                return;
            }
        }
        else if (requiresAuthorization)
        {
            await SendUnauthorizedResponse(context, "Authorization token is missing", ErrorCodeEnum.Unauthorized);
            return;
        }
            
        await _next(context);
    }
    
    /// <summary>
    /// Attach user information to the HttpContext if token is valid
    /// </summary>
    private bool AttachUserToContext(HttpContext context, IJwtService jwtService, string token)
    {
        try
        {
            if (jwtService.ValidateToken(token))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var claims = jwtToken.Claims.ToList();
                
                var userId = claims.FirstOrDefault(x => 
                    x.Type == ClaimTypes.NameIdentifier || 
                    x.Type == "sub" || 
                    x.Type == "nameid")?.Value;
                    
                if (!string.IsNullOrEmpty(userId))
                {
                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Send a 401 Unauthorized response
    /// </summary>
    private async Task SendUnauthorizedResponse(HttpContext context, string message, ErrorCodeEnum errorCodeEnum)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";
        
        var result = Result.Failure(message, errorCodeEnum);
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}
