using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Controllers;
using SharedLibrary.Commons.Interfaces;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Configurations;
using Swashbuckle.AspNetCore.Annotations;
using UserService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace UserService.API.Controllers;

/// <summary>
/// User Service specific file upload controller
/// Handles profile images and user-related file uploads
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public class FileUploadController : BaseFileUploadController
{
    private readonly UserDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public FileUploadController(
        IFileStorageService fileStorageService,
        ILogger<FileUploadController> logger,
        UserDbContext dbContext,
        ICurrentUserService currentUserService) 
        : base(fileStorageService, logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Upload user avatar/profile image
    /// </summary>
    /// <param name="file">Image file (jpg, jpeg, png, webp)</param>
    /// <returns>Uploaded avatar URL</returns>
    [HttpPost("avatar")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Upload user avatar",
        Description = "Upload user profile image to S3. Supported formats: jpg, jpeg, png, webp. Max size: 5MB",
        OperationId = "UploadUserAvatar",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvatarUploadResponse>> UploadAvatar(IFormFile file)
    {
        try
        {
            // Get current user ID
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Validate image file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
            }

            // Validate file size (5MB max for avatars)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 5MB limit for avatars" });
            }

            _logger.LogInformation("User {UserId} uploading avatar: {FileName}, Size: {Size} bytes", 
                userId, file.FileName, file.Length);

            // Delete old avatar if exists
            var userProfile = await _dbContext.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userProfile == null)
            {
                return NotFound(new { message = "User profile not found" });
            }

            if (!string.IsNullOrEmpty(userProfile.AvatarPath))
            {
                _logger.LogInformation("Deleting old avatar for user {UserId}: {OldAvatar}", 
                    userId, userProfile.AvatarPath);
                await _fileStorageService.DeleteFileAsync(userProfile.AvatarPath);
            }

            // Upload new avatar
            var avatarUrl = await _fileStorageService.UploadFileAsync(
                file, 
                $"users/avatars/{userId}", 
                makePublic: true
            );

            // Update user profile with new avatar URL
            userProfile.AvatarPath = avatarUrl;
            userProfile.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Avatar uploaded successfully for user {UserId}: {AvatarUrl}", 
                userId, avatarUrl);

            return Ok(new AvatarUploadResponse
            {
                Success = true,
                AvatarUrl = avatarUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                Message = "Avatar uploaded successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error uploading avatar for user {UserId}", 
                _currentUserService.UserId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", 
                _currentUserService.UserId);
            return StatusCode(500, new { message = "Failed to upload avatar", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user avatar
    /// </summary>
    /// <returns>Deletion status</returns>
    [HttpDelete("avatar")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Delete user avatar",
        Description = "Remove user profile image from S3",
        OperationId = "DeleteUserAvatar",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileDeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDeleteResponse>> DeleteAvatar()
    {
        try
        {
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userProfile = await _dbContext.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userProfile == null)
            {
                return NotFound(new { message = "User profile not found" });
            }

            if (string.IsNullOrEmpty(userProfile.AvatarPath))
            {
                return Ok(new FileDeleteResponse
                {
                    Success = false,
                    Message = "No avatar to delete"
                });
            }

            // Delete from S3
            var deleted = await _fileStorageService.DeleteFileAsync(userProfile.AvatarPath);

            // Update user profile
            userProfile.AvatarPath = null;
            userProfile.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return Ok(new FileDeleteResponse
            {
                Success = deleted,
                Message = deleted ? "Avatar deleted successfully" : "Failed to delete avatar from storage"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", 
                _currentUserService.UserId);
            return StatusCode(500, new { message = "Failed to delete avatar", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload document for creator application
    /// </summary>
    /// <param name="file">Document file (pdf, doc, docx, txt)</param>
    /// <returns>Uploaded document URL</returns>
    [HttpPost("application-document")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Upload creator application document",
        Description = "Upload supporting documents for creator application. Supported formats: pdf, doc, docx, txt. Max size: 10MB",
        OperationId = "UploadApplicationDocument",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FileUploadResponse>> UploadApplicationDocument(IFormFile file)
    {
        try
        {
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Validate document file
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
            }

            // Validate file size (10MB max for documents)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 10MB limit" });
            }

            return await UploadFile(file, $"users/applications/{userId}", makePublic: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading application document for user {UserId}", 
                _currentUserService.UserId);
            return StatusCode(500, new { message = "Failed to upload document", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate presigned URL for secure file access (for private files)
    /// </summary>
    /// <param name="fileUrl">Full S3 file URL</param>
    /// <param name="expirationMinutes">URL expiration time (default: 60 minutes, max: 7 days)</param>
    /// <returns>Temporary presigned URL</returns>
    [HttpPost("presigned-url")]
    [DistributedAuthorize]
    [SwaggerOperation(
        Summary = "Generate presigned URL for file access",
        Description = "Generate temporary presigned URL for accessing private files. URL expires after specified time.",
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
        try
        {
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            return await GeneratePresignedUrl(request.FileUrl, request.ExpirationMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for user {UserId}", 
                _currentUserService.UserId);
            return StatusCode(500, new { message = "Failed to generate presigned URL", error = ex.Message });
        }
    }

    /// <summary>
    /// Test S3 configuration (no authentication required for testing)
    /// </summary>
    [HttpPost("test-s3")]
    [SwaggerOperation(
        Summary = "Test S3 configuration",
        Description = "Test S3 configuration without authentication",
        OperationId = "TestS3Config",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestS3Config(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            _logger.LogInformation("Testing S3 upload: {FileName}, Size: {Size} bytes", 
                file.FileName, file.Length);

            // Test upload to S3
            var fileUrl = await _fileStorageService.UploadFileAsync(
                file, 
                "test", 
                makePublic: true
            );

            _logger.LogInformation("S3 upload test successful: {FileUrl}", fileUrl);

            return Ok(new 
            { 
                success = true,
                message = "S3 configuration is working!",
                fileUrl = fileUrl,
                fileName = file.FileName,
                fileSize = file.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 configuration test failed");
            return StatusCode(500, new 
            { 
                success = false,
                message = "S3 configuration test failed", 
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Debug S3 configuration (no authentication required for testing)
    /// </summary>
    [HttpGet("debug-s3")]
    [SwaggerOperation(
        Summary = "Debug S3 configuration",
        Description = "Debug S3 configuration without authentication",
        OperationId = "DebugS3Config",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult DebugS3Config()
    {
        try
        {
            var config = _fileStorageService.GetType().GetField("_config", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (config?.GetValue(_fileStorageService) is AwsS3Config awsConfig)
            {
                return Ok(new 
                { 
                    success = true,
                    message = "S3 configuration debug info",
                    config = new 
                    {
                        AccessKey = awsConfig.AccessKey?.Substring(0, Math.Min(8, awsConfig.AccessKey.Length)) + "...",
                        SecretKey = awsConfig.SecretKey?.Substring(0, Math.Min(8, awsConfig.SecretKey.Length)) + "...",
                        Region = awsConfig.Region,
                        BucketName = awsConfig.BucketName,
                        CloudFrontUrl = awsConfig.CloudFrontUrl,
                        EnableEncryption = awsConfig.EnableEncryption,
                        DefaultAcl = awsConfig.DefaultAcl,
                        MaxFileSizeBytes = awsConfig.MaxFileSizeBytes,
                        AllowedExtensions = awsConfig.AllowedExtensions
                    }
                });
            }
            
            return Ok(new 
            { 
                success = false,
                message = "Could not retrieve S3 configuration"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false,
                message = "Debug failed", 
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test presigned URL generation (no authentication required for testing)
    /// </summary>
    [HttpPost("test-presigned-url")]
    [SwaggerOperation(
        Summary = "Test presigned URL generation",
        Description = "Test presigned URL generation without authentication",
        OperationId = "TestPresignedUrl",
        Tags = new[] { "File Upload" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestPresignedUrl([FromBody] PresignedUrlRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FileUrl))
            {
                return BadRequest(new { message = "File URL is required" });
            }

            if (request.ExpirationMinutes <= 0 || request.ExpirationMinutes > 10080)
            {
                return BadRequest(new { message = "Expiration time must be between 1 minute and 7 days (10080 minutes)" });
            }

            _logger.LogInformation("Testing presigned URL generation for: {FileUrl}, Expiration: {Minutes} minutes",
                request.FileUrl, request.ExpirationMinutes);

            // Check if file exists
            var exists = await _fileStorageService.FileExistsAsync(request.FileUrl);
            if (!exists)
            {
                return NotFound(new { message = "File not found" });
            }

            // Extract key from URL
            var uri = new Uri(request.FileUrl);
            var fileKey = uri.AbsolutePath.TrimStart('/');

            // Generate presigned URL
            var presignedUrl = await _fileStorageService.GetPresignedUrlAsync(
                fileKey,
                TimeSpan.FromMinutes(request.ExpirationMinutes)
            );

            return Ok(new 
            { 
                success = true,
                message = "Presigned URL generated successfully",
                presignedUrl = presignedUrl,
                originalUrl = request.FileUrl,
                expirationMinutes = request.ExpirationMinutes,
                expiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing presigned URL generation for: {FileUrl}", request.FileUrl);
            return StatusCode(500, new 
            { 
                success = false,
                message = "Failed to generate presigned URL", 
                error = ex.Message
            });
        }
    }
}

#region Response DTOs

/// <summary>
/// Response for avatar upload
/// </summary>
public class AvatarUploadResponse
{
    public bool Success { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request for generating presigned URL
/// </summary>
public class PresignedUrlRequest
{
    public string FileUrl { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

#endregion
