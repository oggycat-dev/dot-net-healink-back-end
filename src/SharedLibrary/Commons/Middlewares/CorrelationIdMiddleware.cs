using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Configurations;

namespace SharedLibrary.Commons.Middlewares;

/// <summary>
/// Middleware to handle correlation ID for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        // Extract correlation ID from incoming request or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        // Set correlation ID in service
        correlationIdService.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to HttpContext for easy access
        context.Items["CorrelationId"] = correlationId;

        // Log with correlation ID
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method
        }))
        {
            _logger.LogInformation("Processing request with CorrelationId: {CorrelationId}", correlationId);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in request with CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
            finally
            {
                _logger.LogDebug("Completed request with CorrelationId: {CorrelationId}", correlationId);
            }
        }
    }
}

/// <summary>
/// Extension methods for CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
