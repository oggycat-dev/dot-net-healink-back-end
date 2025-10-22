using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SharedLibrary.Commons.Middlewares;

/// <summary>
/// Middleware to automatically transform S3 URLs to presigned URLs in API responses
/// This ensures frontend always receives valid, time-limited URLs for accessing private S3 objects
/// </summary>
public class S3UrlTransformationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<S3UrlTransformationMiddleware> _logger;
    
    // Patterns to detect S3 URLs in different formats
    private static readonly Regex S3UrlPattern = new(
        @"https?://([^/]+\.)?s3[.-]([^/]+\.)?amazonaws\.com/[^\s""'}]+|" +
        @"https?://[^/]+\.cloudfront\.net/[^\s""'}]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    
    // JSON property names that typically contain file URLs
    private static readonly HashSet<string> UrlPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "thumbnailUrl", "audioUrl", "imageUrl", "fileUrl", "avatarUrl", 
        "documentUrl", "videoUrl", "coverUrl", "bannerUrl", "attachmentUrl"
    };
    
    // Expiration time for presigned URLs
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(1);

    public S3UrlTransformationMiddleware(
        RequestDelegate next,
        ILogger<S3UrlTransformationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IFileStorageService fileStorageService)
    {
        // Only process GET requests - check Content-Type later after response is generated
        if (context.Request.Method != HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        // Capture the original response body
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Execute the next middleware
            await _next(context);

            // Check if response should be transformed (after Content-Type is set)
            if (!ShouldTransform(context) || context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            {
                // Not a JSON response or not successful, return original
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                return;
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

            // Check if response contains S3 URLs
            if (string.IsNullOrEmpty(responseContent) || !ContainsS3Urls(responseContent))
            {
                // No transformation needed, return original response
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                return;
            }

            // Transform S3 URLs to presigned URLs
            var transformedContent = await TransformS3Urls(responseContent, fileStorageService);

            // Add CORS headers for S3 presigned URLs (allows Flutter Web to fetch images/audio)
            AddCorsHeaders(context.Response);

            // Write transformed content back
            var bytes = Encoding.UTF8.GetBytes(transformedContent);
            context.Response.ContentLength = bytes.Length;
            context.Response.Body = originalBodyStream;
            await context.Response.Body.WriteAsync(bytes);
            
            _logger.LogDebug("S3 URLs transformed in response for {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming S3 URLs in response");
            
            // On error, return original response
            responseBody.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private static void AddCorsHeaders(HttpResponse response)
    {
        // Add CORS headers to allow Flutter Web and other browsers to access S3 presigned URLs
        response.Headers["Access-Control-Allow-Origin"] = "*";
        response.Headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        response.Headers["Access-Control-Expose-Headers"] = "Content-Length, Content-Range";
    }

    private static bool ShouldTransform(HttpContext context)
    {
        // Only transform JSON responses
        var contentType = context.Response.ContentType;
        return contentType != null && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsS3Urls(string content)
    {
        return S3UrlPattern.IsMatch(content);
    }

    private async Task<string> TransformS3Urls(string jsonContent, IFileStorageService fileStorageService)
    {
        try
        {
            // Parse JSON to identify S3 URLs
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Extract all S3 URLs
            var s3Urls = ExtractS3Urls(root);
            
            if (s3Urls.Count == 0)
                return jsonContent;

            // Generate presigned URLs for all S3 URLs in parallel
            var urlTransformations = new Dictionary<string, string>();
            var tasks = s3Urls.Select(async url =>
            {
                try
                {
                    var presignedUrl = await GeneratePresignedUrl(url, fileStorageService);
                    return (Original: url, Presigned: presignedUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate presigned URL for: {Url}", url);
                    return (Original: url, Presigned: url); // Keep original on error
                }
            });

            var results = await Task.WhenAll(tasks);
            
            foreach (var (original, presigned) in results)
            {
                urlTransformations[original] = presigned;
            }

            // Replace all S3 URLs with presigned URLs
            var transformedContent = jsonContent;
            foreach (var (original, presigned) in urlTransformations)
            {
                transformedContent = transformedContent.Replace(original, presigned);
            }

            return transformedContent;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON for S3 URL transformation");
            return jsonContent; // Return original on parse error
        }
    }

    private static HashSet<string> ExtractS3Urls(JsonElement element, HashSet<string>? urls = null)
    {
        urls ??= new HashSet<string>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Check if property name suggests it's a URL field
                    if (property.Value.ValueKind == JsonValueKind.String && 
                        UrlPropertyNames.Contains(property.Name))
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrEmpty(value) && IsS3Url(value))
                        {
                            urls.Add(value);
                        }
                    }
                    else
                    {
                        ExtractS3Urls(property.Value, urls);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractS3Urls(item, urls);
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                if (!string.IsNullOrEmpty(stringValue) && IsS3Url(stringValue))
                {
                    urls.Add(stringValue);
                }
                break;
        }

        return urls;
    }

    private static bool IsS3Url(string url)
    {
        return S3UrlPattern.IsMatch(url);
    }

    private async Task<string> GeneratePresignedUrl(string s3Url, IFileStorageService fileStorageService)
    {
        try
        {
            // Extract file key from S3 URL
            var uri = new Uri(s3Url);
            string key;
            
            // If it's a CloudFront URL, use full path as key
            if (uri.Host.Contains("cloudfront.net"))
            {
                key = uri.AbsolutePath.TrimStart('/');
            }
            // If it's S3 URL, determine URL style and extract key
            else if (uri.Host.Contains("s3") && uri.Host.Contains("amazonaws.com"))
            {
                // Check if it's path-style or virtual-hosted-style URL
                // Path-style: https://s3.region.amazonaws.com/bucket-name/key
                // Virtual-hosted-style: https://bucket-name.s3.region.amazonaws.com/key
                
                var hostParts = uri.Host.Split('.');
                var isVirtualHostedStyle = hostParts.Length > 2 && hostParts[0] != "s3";
                
                if (isVirtualHostedStyle)
                {
                    // Virtual-hosted-style: bucket in hostname, path is the key
                    key = uri.AbsolutePath.TrimStart('/');
                }
                else
                {
                    // Path-style: bucket in path, skip first segment
                    var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    key = segments.Length > 1 ? string.Join('/', segments.Skip(1)) : segments[0];
                }
            }
            else
            {
                // Unknown format, use full path
                key = uri.AbsolutePath.TrimStart('/');
            }

            // Generate presigned URL
            var presignedUrl = await fileStorageService.GetPresignedUrlAsync(key, DefaultExpiration);
            
            _logger.LogDebug("Generated presigned URL for key: {Key} from URL: {Url}", key, s3Url);
            
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned URL for: {Url}", s3Url);
            return s3Url; // Return original URL on error
        }
    }
}
