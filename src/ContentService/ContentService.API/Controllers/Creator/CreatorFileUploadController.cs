using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Controllers;
using SharedLibrary.Commons.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ContentService.API.Controllers.Creator;

/// <summary>
/// Creator File Upload Controller - For content creators to upload files for their content
/// </summary>
[ApiController]
[Route("api/creator/upload")]
[DistributedAuthorize(Roles = new[] { "Admin", "ContentCreator" })]
[ApiExplorerSettings(GroupName = "Creator")]
public class CreatorFileUploadController : BaseFileUploadController
{
    public CreatorFileUploadController(
        IFileStorageService fileStorageService,
        ILogger<CreatorFileUploadController> logger)
        : base(fileStorageService, logger)
    {
    }

    /// <summary>
    /// Upload podcast audio file
    /// </summary>
    [HttpPost("podcast/audio")]
    [SwaggerOperation(
        Summary = "Upload podcast audio file",
        Description = "Upload podcast audio file to S3 storage. Supported formats: mp3, wav, m4a, aac. Max size: 500MB",
        OperationId = "UploadPodcastAudio",
        Tags = new[] { "Creator File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(500_000_000)] // 500MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<ActionResult<FileUploadResponse>> UploadPodcastAudio(IFormFile file)
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
    /// Upload podcast thumbnail image
    /// </summary>
    [HttpPost("podcast/thumbnail")]
    [SwaggerOperation(
        Summary = "Upload podcast thumbnail",
        Description = "Upload podcast thumbnail image to S3. Supported formats: jpg, png, webp. Max size: 10MB",
        OperationId = "UploadPodcastThumbnail",
        Tags = new[] { "Creator File Upload" }
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
    /// Upload podcast transcript file
    /// </summary>
    [HttpPost("podcast/transcript")]
    [SwaggerOperation(
        Summary = "Upload podcast transcript",
        Description = "Upload podcast transcript file. Supported formats: txt, pdf, docx, srt",
        OperationId = "UploadPodcastTranscript",
        Tags = new[] { "Creator File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadPodcastTranscript(IFormFile file)
    {
        // Validate document file
        var allowedExtensions = new[] { ".txt", ".pdf", ".docx", ".doc", ".srt" };
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
    [SwaggerOperation(
        Summary = "Upload flashcard image",
        Description = "Upload flashcard image to S3. Supported formats: jpg, png, webp",
        OperationId = "UploadFlashcardImage",
        Tags = new[] { "Creator File Upload" }
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
    [SwaggerOperation(
        Summary = "Upload postcard image",
        Description = "Upload postcard image to S3. Supported formats: jpg, png, webp",
        OperationId = "UploadPostcardImage",
        Tags = new[] { "Creator File Upload" }
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
    [DistributedAuthorize] // Any authenticated user
    [SwaggerOperation(
        Summary = "Upload community story image",
        Description = "Upload image for community story. Supported formats: jpg, png, webp",
        OperationId = "UploadCommunityImage",
        Tags = new[] { "Creator File Upload" }
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
    /// Generate presigned URL for secure file access
    /// </summary>
    [HttpPost("presigned-url")]
    [SwaggerOperation(
        Summary = "Generate presigned URL for file access",
        Description = "Generate temporary presigned URL for accessing private content files. URL expires after specified time.",
        OperationId = "GeneratePresignedUrl",
        Tags = new[] { "Creator File Upload" }
    )]
    [ProducesResponseType(typeof(PresignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PresignedUrlResponse>> GetPresignedUrl(
        [FromBody] PresignedUrlRequest request)
    {
        return await GeneratePresignedUrl(request.FileUrl, request.ExpirationMinutes);
    }
}

#region Request DTOs

public class PresignedUrlRequest
{
    public string FileUrl { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

#endregion
