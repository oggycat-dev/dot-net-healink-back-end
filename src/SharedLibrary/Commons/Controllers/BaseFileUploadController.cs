using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Interfaces;

namespace SharedLibrary.Commons.Controllers;

/// <summary>
/// Abstract base controller for file upload operations
/// Services should inherit from this class to implement their own file upload endpoints
/// </summary>
public abstract class BaseFileUploadController : ControllerBase
{
    protected readonly IFileStorageService _fileStorageService;
    protected readonly ILogger _logger;

    protected BaseFileUploadController(
        IFileStorageService fileStorageService,
        ILogger logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single file to storage
    /// </summary>
    protected async Task<ActionResult<FileUploadResponse>> UploadFile(
        IFormFile file,
        string? folderPath = null,
        bool makePublic = true)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided or file is empty" });
            }

            _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes, Folder: {Folder}", 
                file.FileName, file.Length, folderPath ?? "root");

            var fileUrl = await _fileStorageService.UploadFileAsync(file, folderPath, makePublic);

            return Ok(new FileUploadResponse
            {
                Success = true,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                Message = "File uploaded successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error uploading file: {FileName}", file?.FileName);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return StatusCode(500, new { message = "Failed to upload file", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload multiple files to storage
    /// </summary>
    protected async Task<ActionResult<MultipleFileUploadResponse>> UploadMultipleFiles(
        List<IFormFile> files,
        string? folderPath = null,
        bool makePublic = true)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files provided" });
            }

            _logger.LogInformation("Uploading {Count} files to folder: {Folder}", 
                files.Count, folderPath ?? "root");

            var fileUrls = await _fileStorageService.UploadFilesAsync(files, folderPath, makePublic);

            return Ok(new MultipleFileUploadResponse
            {
                Success = true,
                FileUrls = fileUrls,
                TotalFiles = files.Count,
                SuccessfulUploads = fileUrls.Count,
                FailedUploads = files.Count - fileUrls.Count,
                Message = $"Uploaded {fileUrls.Count} out of {files.Count} files successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(500, new { message = "Failed to upload files", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    protected async Task<ActionResult<FileDeleteResponse>> DeleteFile(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(new { message = "File URL is required" });
            }

            _logger.LogInformation("Deleting file: {FileUrl}", fileUrl);

            var success = await _fileStorageService.DeleteFileAsync(fileUrl);

            return Ok(new FileDeleteResponse
            {
                Success = success,
                Message = success ? "File deleted successfully" : "File not found or could not be deleted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "Failed to delete file", error = ex.Message });
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    protected async Task<ActionResult<FileMetadata>> GetFileMetadata(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(new { message = "File URL is required" });
            }

            _logger.LogInformation("Getting metadata for file: {FileUrl}", fileUrl);

            var metadata = await _fileStorageService.GetFileMetadataAsync(fileUrl);

            if (metadata == null)
            {
                return NotFound(new { message = "File not found" });
            }

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "Failed to get file metadata", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate presigned URL for private file access (for frontend)
    /// </summary>
    protected async Task<ActionResult<PresignedUrlResponse>> GeneratePresignedUrl(
        string fileUrl, 
        int expirationMinutes = 60)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(new { message = "File URL is required" });
            }

            if (expirationMinutes <= 0 || expirationMinutes > 10080) // Max 7 days
            {
                return BadRequest(new { message = "Expiration time must be between 1 minute and 7 days (10080 minutes)" });
            }

            _logger.LogInformation("Generating presigned URL for: {FileUrl}, Expiration: {Minutes} minutes", 
                fileUrl, expirationMinutes);

            // Check if file exists
            var exists = await _fileStorageService.FileExistsAsync(fileUrl);
            if (!exists)
            {
                return NotFound(new { message = "File not found" });
            }

            // Extract key from URL
            var uri = new Uri(fileUrl);
            var fileKey = uri.AbsolutePath.TrimStart('/');

            // Generate presigned URL
            var presignedUrl = await _fileStorageService.GetPresignedUrlAsync(
                fileKey, 
                TimeSpan.FromMinutes(expirationMinutes)
            );

            return Ok(new PresignedUrlResponse
            {
                Success = true,
                PresignedUrl = presignedUrl,
                OriginalUrl = fileUrl,
                ExpiresInMinutes = expirationMinutes,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Message = "Presigned URL generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "Failed to generate presigned URL", error = ex.Message });
        }
    }
}

#region Response DTOs

public class FileUploadResponse
{
    public bool Success { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MultipleFileUploadResponse
{
    public bool Success { get; set; }
    public List<string> FileUrls { get; set; } = new();
    public int TotalFiles { get; set; }
    public int SuccessfulUploads { get; set; }
    public int FailedUploads { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FileDeleteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PresignedUrlResponse
{
    public bool Success { get; set; }
    public string PresignedUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
