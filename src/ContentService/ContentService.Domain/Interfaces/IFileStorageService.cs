using Microsoft.AspNetCore.Http;

namespace ContentService.Domain.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to S3 storage and return the public URL
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folderPath">Optional folder path within the bucket</param>
    /// <returns>The public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string? folderPath = null);
    
    /// <summary>
    /// Delete a file from S3 storage using its URL
    /// </summary>
    /// <param name="fileUrl">The public URL of the file to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string fileUrl);
    
    /// <summary>
    /// Get a pre-signed URL for temporary access to a private file
    /// </summary>
    /// <param name="fileKey">The S3 object key</param>
    /// <param name="expirationTime">URL expiration time</param>
    /// <returns>Pre-signed URL</returns>
    Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expirationTime);
    
    /// <summary>
    /// Check if a file exists in S3 storage
    /// </summary>
    /// <param name="fileUrl">The public URL of the file</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string fileUrl);
}