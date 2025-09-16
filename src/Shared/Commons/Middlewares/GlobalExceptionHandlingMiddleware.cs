using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
// using Microsoft.Data.SqlClient; // Not needed for PostgreSQL
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProductAuthMicroservice.Commons.Enums;
using ProductAuthMicroservice.Commons.Exceptions;
using ProductAuthMicroservice.Commons.Models;
using Microsoft.AspNetCore.Builder;

namespace ProductAuthMicroservice.Commons.Middlewares;

/// <summary>
/// Middleware for handling exceptions globally across microservices
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}, User: {User}, " +
                "Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                context.Request.Path, 
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous",
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace);
            
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, errors) = ClassifyException(exception);
        
        context.Response.StatusCode = (int)statusCode;
        var result = Result.Failure(message, errorCode, errors);
        context.Response.ContentType = "application/json";
        
        return context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
    
    private (HttpStatusCode statusCode, ErrorCodeEnum errorCode, string message, List<string>? errors) ClassifyException(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException fluentValidationEx => (
                HttpStatusCode.BadRequest,
                ErrorCodeEnum.ValidationFailed,
                "Validation failed",
                fluentValidationEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                ErrorCodeEnum.ValidationFailed,
                "Validation failed",
                validationEx.Errors.SelectMany(e => e.Value).ToList()
            ),
            
            ArgumentException => (
                HttpStatusCode.BadRequest,
                ErrorCodeEnum.InvalidInput,
                "Invalid request parameters",
                null
            ),
            
            UnauthorizedAccessException unauthorizedEx when IsFileAccess(unauthorizedEx) => (
                HttpStatusCode.Forbidden,
                ErrorCodeEnum.StorageError,
                "File access denied",
                null
            ),
            
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ErrorCodeEnum.Unauthorized,
                "Authentication required",
                null
            ),
            
            ForbiddenAccessException => (
                HttpStatusCode.Forbidden,
                ErrorCodeEnum.Forbidden,
                "Access denied",
                null
            ),
            
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ErrorCodeEnum.NotFound,
                "Resource not found",
                null
            ),
            
            // PostgreSQL specific exceptions
            NpgsqlException npgsqlEx => HandlePostgreSqlException(npgsqlEx),
            DbUpdateException dbEx => HandleDatabaseException(dbEx),
            InvalidOperationException invalidEx when IsDatabaseRelated(invalidEx) => HandleDatabaseException(invalidEx),
            
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                ErrorCodeEnum.ExternalServiceError,
                "Request timeout - please try again later",
                null
            ),
            
            FileNotFoundException => (
                HttpStatusCode.NotFound,
                ErrorCodeEnum.FileNotFound,
                "File not found",
                null
            ),
            
            DirectoryNotFoundException => (
                HttpStatusCode.NotFound,
                ErrorCodeEnum.FileNotFound,
                "Directory not found", 
                null
            ),
            
            HttpRequestException => (
                HttpStatusCode.BadGateway,
                ErrorCodeEnum.ExternalServiceError,
                "External service unavailable",
                null
            ),
            
            _ => (
                HttpStatusCode.InternalServerError,
                ErrorCodeEnum.InternalError,
                "An internal server error occurred",
                null
            )
        };
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandlePostgreSqlException(NpgsqlException npgsqlEx)
    {
        return npgsqlEx.SqlState switch
        {
            // Connection issues
            "08000" or "08003" or "08006" or "08001" or "08004" => (
                HttpStatusCode.ServiceUnavailable,
                ErrorCodeEnum.DatabaseError,
                "Database service temporarily unavailable",
                null
            ),
            
            // Authentication failed
            "28000" or "28P01" => (
                HttpStatusCode.ServiceUnavailable,
                ErrorCodeEnum.DatabaseError,
                "Database authentication failed",
                null
            ),
            
            // Foreign key violation
            "23503" => (
                HttpStatusCode.Conflict,
                ErrorCodeEnum.ResourceConflict,
                "Operation violates data constraints",
                null
            ),
            
            // Unique violation
            "23505" => (
                HttpStatusCode.Conflict,
                ErrorCodeEnum.DuplicateEntry,
                "Duplicate entry found",
                null
            ),
            
            // Check constraint violation
            "23514" => (
                HttpStatusCode.BadRequest,
                ErrorCodeEnum.ValidationFailed,
                "Data violates constraints",
                null
            ),
            
            _ => (
                HttpStatusCode.InternalServerError,
                ErrorCodeEnum.DatabaseError,
                "Database operation failed",
                null
            )
        };
    }
    
    private (HttpStatusCode, ErrorCodeEnum, string, List<string>?) HandleDatabaseException(Exception exception)
    {
        return exception switch
        {
            DbUpdateException => (
                HttpStatusCode.Conflict,
                ErrorCodeEnum.ResourceConflict,
                "Data update conflict occurred",
                null
            ),
            
            _ => (
                HttpStatusCode.InternalServerError,
                ErrorCodeEnum.DatabaseError,
                "Database error occurred",
                null
            )
        };
    }
    
    private bool IsDatabaseRelated(Exception exception)
    {
        var message = exception.Message.ToLowerInvariant();
        return message.Contains("database") || 
               message.Contains("connection") || 
               message.Contains("sql") ||
               message.Contains("entity framework") ||
               message.Contains("dbcontext") ||
               message.Contains("npgsql") ||
               message.Contains("postgres");
    }
    
    private bool IsFileAccess(Exception exception)
    {
        return exception.Message.Contains("file") || 
               exception.Message.Contains("directory") ||
               exception.Message.Contains("path");
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
