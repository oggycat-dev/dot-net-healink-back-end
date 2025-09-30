using Microsoft.AspNetCore.Http;

namespace SharedLibrary.Commons.Interfaces;

/// <summary>
/// Interface for file storage operations with cloud storage (S3, Azure Blob, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to cloud storage and return the public URL
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folderPath">Optional folder path within the bucket/container</param>
    /// <param name="makePublic">Whether to make the file publicly accessible</param>
    /// <returns>The URL of the uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string? folderPath = null, bool makePublic = true);
    
    /// <summary>
    /// Upload multiple files to cloud storage
    /// </summary>
    /// <param name="files">The files to upload</param>
    /// <param name="folderPath">Optional folder path within the bucket/container</param>
    /// <param name="makePublic">Whether to make the files publicly accessible</param>
    /// <returns>List of URLs of the uploaded files</returns>
    Task<List<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string? folderPath = null, bool makePublic = true);
    
    /// <summary>
    /// Delete a file from cloud storage using its URL
    /// </summary>
    /// <param name="fileUrl">The public URL of the file to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string fileUrl);
    
    /// <summary>
    /// Delete multiple files from cloud storage
    /// </summary>
    /// <param name="fileUrls">The public URLs of the files to delete</param>
    /// <returns>Number of successfully deleted files</returns>
    Task<int> DeleteFilesAsync(IEnumerable<string> fileUrls);
    
    /// <summary>
    /// Get a pre-signed URL for temporary access to a private file
    /// </summary>
    /// <param name="fileKey">The storage object key</param>
    /// <param name="expirationTime">URL expiration time</param>
    /// <returns>Pre-signed URL</returns>
    Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expirationTime);
    
    /// <summary>
    /// Check if a file exists in cloud storage
    /// </summary>
    /// <param name="fileUrl">The public URL of the file</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string fileUrl);
    
    /// <summary>
    /// Get file metadata (size, content type, etc.)
    /// </summary>
    /// <param name="fileUrl">The public URL of the file</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata?> GetFileMetadataAsync(string fileUrl);
    
    /// <summary>
    /// Copy a file to a new location
    /// </summary>
    /// <param name="sourceFileUrl">Source file URL</param>
    /// <param name="destinationKey">Destination key/path</param>
    /// <returns>New file URL</returns>
    Task<string> CopyFileAsync(string sourceFileUrl, string destinationKey);
}

/// <summary>
/// File metadata information
/// </summary>
public class FileMetadata
{
    public string FileName { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
