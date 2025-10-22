using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Controllers;
using SharedLibrary.Commons.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ContentService.API.Controllers;

/// <summary>
/// Content Service specific file upload controller
/// Inherits from BaseFileUploadController
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public class FileUploadController : BaseFileUploadController
{
    public FileUploadController(
        IFileStorageService fileStorageService,
        ILogger<FileUploadController> logger) 
        : base(fileStorageService, logger)
    {
    }

    /// <summary>
    /// Upload podcast audio file
    /// </summary>
    [HttpPost("podcast")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Upload podcast audio file",
        Description = "Upload podcast audio file to S3 storage. Supported formats: mp3, wav, m4a",
        OperationId = "UploadPodcastFile",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FileUploadResponse>> UploadPodcastFile(IFormFile file)
    {
        // Validate audio file
        var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "podcasts/audio", makePublic: true);
    }

    /// <summary>
    /// Upload podcast thumbnail
    /// </summary>
    [HttpPost("podcast/thumbnail")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Upload podcast thumbnail",
        Description = "Upload podcast thumbnail image to S3. Supported formats: jpg, png, webp",
        OperationId = "UploadPodcastThumbnail",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadPodcastThumbnail(IFormFile file)
    {
        // Validate image file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "podcasts/thumbnails", makePublic: true);
    }

    /// <summary>
    /// Upload podcast transcript
    /// </summary>
    [HttpPost("podcast/transcript")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Upload podcast transcript",
        Description = "Upload podcast transcript file. Supported formats: txt, pdf, docx",
        OperationId = "UploadPodcastTranscript",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadPodcastTranscript(IFormFile file)
    {
        // Validate document file
        var allowedExtensions = new[] { ".txt", ".pdf", ".docx", ".doc" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "podcasts/transcripts", makePublic: true);
    }

    /// <summary>
    /// Upload flashcard image
    /// </summary>
    [HttpPost("flashcard")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Upload flashcard image",
        Description = "Upload flashcard image to S3. Supported formats: jpg, png, webp",
        OperationId = "UploadFlashcardImage",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadFlashcardImage(IFormFile file)
    {
        // Validate image file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "flashcards", makePublic: true);
    }

    /// <summary>
    /// Upload postcard image
    /// </summary>
    [HttpPost("postcard")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Upload postcard image",
        Description = "Upload postcard image to S3. Supported formats: jpg, png, webp",
        OperationId = "UploadPostcardImage",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadPostcardImage(IFormFile file)
    {
        // Validate image file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "postcards", makePublic: true);
    }

    /// <summary>
    /// Upload community story image
    /// </summary>
    [HttpPost("community/image")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Upload community story image",
        Description = "Upload image for community story. Supported formats: jpg, png, webp",
        OperationId = "UploadCommunityImage",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadCommunityImage(IFormFile file)
    {
        // Validate image file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        return await UploadFile(file, "community", makePublic: true);
    }

    /// <summary>
    /// Generate presigned URL for secure file access (for private content files)
    /// </summary>
    /// <param name="request">Request containing file URL and expiration time</param>
    /// <returns>Temporary presigned URL</returns>
    [HttpPost("presigned-url")]
    [DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
    [SwaggerOperation(
        Summary = "Generate presigned URL for file access",
        Description = "Generate temporary presigned URL for accessing private content files. URL expires after specified time.",
        OperationId = "GeneratePresignedUrl",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(PresignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresignedUrlResponse>> GetPresignedUrl(
        [FromBody] PresignedUrlRequest request)
    {
        return await GeneratePresignedUrl(request.FileUrl, request.ExpirationMinutes);
    }

    /// <summary>
    /// Download file from S3 through backend proxy (fixes CORS for Flutter web)
    /// </summary>
    /// <remarks>
    /// This endpoint proxies S3 file downloads through the backend with CORS headers.
    /// Solves the issue where Flutter web cannot load images/audio from S3 presigned URLs due to CORS policy.
    /// </remarks>
    [HttpGet("proxy")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Download file from S3 proxy",
        Description = "Proxies file downloads from S3 through the backend with proper CORS headers for web clients",
        OperationId = "ProxyS3Download",
        Tags = new[] { "File Download" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ProxyDownloadFile(
        [FromQuery] string url,
        [FromQuery] string? contentType = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { message = "URL parameter is required" });
        }

        try
        {
            // Validate URL is S3 (security check)
            // Support both regional S3 URLs (s3.region.amazonaws.com) and legacy (s3.amazonaws.com, s3-)
            if (!url.Contains(".s3.") && !url.Contains(".s3-") && !url.Contains("://s3.") && !url.Contains("://s3-"))
            {
                return BadRequest(new { message = "Invalid S3 URL" });
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("S3 proxy download failed for URL: {Url}, Status: {Status}", url, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { message = "Failed to download file from S3" });
            }

            var stream = await response.Content.ReadAsStreamAsync();
            
            // Determine content type
            var type = contentType ?? response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            
            // Add CORS headers to response (browser will see these)
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";

            _logger.LogInformation("S3 proxy download succeeded for URL: {Url}, ContentType: {Type}", url, type);

            return File(stream, type, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying S3 download for URL: {Url}", url);
            return StatusCode(500, new { message = "Error downloading file", error = ex.Message });
        }
    }
}

#region Request DTOs

/// <summary>
/// Request for generating presigned URL
/// </summary>
public class PresignedUrlRequest
{
    public string FileUrl { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

#endregion
