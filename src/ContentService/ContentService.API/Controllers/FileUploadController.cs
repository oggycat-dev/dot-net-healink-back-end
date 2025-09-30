using Microsoft.AspNetCore.Mvc;
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
}
