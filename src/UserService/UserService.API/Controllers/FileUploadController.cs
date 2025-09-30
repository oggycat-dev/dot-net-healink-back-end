using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Controllers;
using SharedLibrary.Commons.Interfaces;
using SharedLibrary.Commons.Services;
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

#endregion
