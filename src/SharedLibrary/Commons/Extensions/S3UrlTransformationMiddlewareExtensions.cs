using Microsoft.AspNetCore.Builder;
using SharedLibrary.Commons.Middlewares;

namespace SharedLibrary.Commons.Extensions;

/// <summary>
/// Extension methods for registering S3 URL transformation middleware
/// </summary>
public static class S3UrlTransformationMiddlewareExtensions
{
    /// <summary>
    /// Add S3 URL transformation middleware to automatically convert S3 URLs to presigned URLs in responses
    /// This ensures frontend receives time-limited, secure URLs for accessing S3 objects
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseS3UrlTransformation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<S3UrlTransformationMiddleware>();
    }
}
