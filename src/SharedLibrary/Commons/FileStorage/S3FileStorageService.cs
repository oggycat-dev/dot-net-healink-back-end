using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.Interfaces;

namespace SharedLibrary.Commons.FileStorage;

/// <summary>
/// AWS S3 implementation of file storage service
/// </summary>
public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Config _config;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<AwsS3Config> config,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Validate configuration
        _config.Validate();
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? folderPath = null, bool makePublic = true)
    {
        try
        {
            ValidateFile(file);

            // Generate unique file key
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = string.IsNullOrEmpty(folderPath) 
                ? uniqueFileName 
                : $"{folderPath.TrimEnd('/')}/{uniqueFileName}";

            _logger.LogInformation("Uploading file to S3: {Key}, Size: {Size} bytes", key, file.Length);

            // Create upload request
            var request = new PutObjectRequest
            {
                BucketName = _config.BucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                // Remove CannedACL as modern S3 buckets don't allow ACLs
                // Use bucket policy instead for public access
                ServerSideEncryptionMethod = _config.EnableEncryption 
                    ? ServerSideEncryptionMethod.AES256 
                    : ServerSideEncryptionMethod.None,
                Metadata =
                {
                    ["original-filename"] = file.FileName,
                    ["upload-timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["content-length"] = file.Length.ToString()
                }
            };

            // Upload file
            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Return CloudFront URL if configured, otherwise S3 URL
                var fileUrl = GetFileUrl(key);

                _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);
                return fileUrl;
            }

            throw new InvalidOperationException($"Failed to upload file. HTTP Status: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading file: {FileName}", file.FileName);
            throw new InvalidOperationException($"S3 error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<List<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string? folderPath = null, bool makePublic = true)
    {
        var uploadedUrls = new List<string>();
        
        foreach (var file in files)
        {
            try
            {
                var url = await UploadFileAsync(file, folderPath, makePublic);
                uploadedUrls.Add(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
                // Continue with other files, but log the error
            }
        }
        
        return uploadedUrls;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return false;

            // Extract key from URL
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Could not extract key from URL: {FileUrl}", fileUrl);
                return false;
            }

            _logger.LogInformation("Deleting file from S3: {Key}", key);

            var request = new DeleteObjectRequest
            {
                BucketName = _config.BucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("File deleted successfully: {Key}", key);
                return true;
            }

            _logger.LogWarning("Failed to delete file. HTTP Status: {StatusCode}", response.HttpStatusCode);
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error deleting file: {FileUrl}", fileUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<int> DeleteFilesAsync(IEnumerable<string> fileUrls)
    {
        int deletedCount = 0;
        
        foreach (var fileUrl in fileUrls)
        {
            if (await DeleteFileAsync(fileUrl))
            {
                deletedCount++;
            }
        }
        
        return deletedCount;
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expirationTime)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _config.BucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.Add(expirationTime),
                Verb = HttpVerb.GET
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            _logger.LogInformation("Generated presigned URL for key: {Key}, expires in {Minutes} minutes", 
                fileKey, expirationTime.TotalMinutes);
            
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for key: {Key}", fileKey);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
                return false;

            var request = new GetObjectMetadataRequest
            {
                BucketName = _config.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string fileUrl)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key))
                return null;

            var request = new GetObjectMetadataRequest
            {
                BucketName = _config.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);

            return new FileMetadata
            {
                FileName = response.Metadata["x-amz-meta-original-filename"] ?? Path.GetFileName(key),
                ContentLength = response.ContentLength,
                ContentType = response.Headers.ContentType,
                LastModified = response.LastModified,
                Metadata = response.Metadata.Keys.ToDictionary(k => k, k => response.Metadata[k])
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<string> CopyFileAsync(string sourceFileUrl, string destinationKey)
    {
        try
        {
            var sourceKey = ExtractKeyFromUrl(sourceFileUrl);
            if (string.IsNullOrEmpty(sourceKey))
                throw new ArgumentException("Invalid source file URL", nameof(sourceFileUrl));

            _logger.LogInformation("Copying file from {SourceKey} to {DestinationKey}", sourceKey, destinationKey);

            var request = new CopyObjectRequest
            {
                SourceBucket = _config.BucketName,
                SourceKey = sourceKey,
                DestinationBucket = _config.BucketName,
                DestinationKey = destinationKey
            };

            var response = await _s3Client.CopyObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var newFileUrl = GetFileUrl(destinationKey);
                _logger.LogInformation("File copied successfully to: {FileUrl}", newFileUrl);
                return newFileUrl;
            }

            throw new InvalidOperationException($"Failed to copy file. HTTP Status: {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file: {SourceUrl} to {DestinationKey}", sourceFileUrl, destinationKey);
            throw;
        }
    }

    #region Private Helper Methods

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is null or empty", nameof(file));

        if (file.Length > _config.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File size ({file.Length} bytes) exceeds maximum allowed size ({_config.MaxFileSizeBytes} bytes)");

        if (!string.IsNullOrEmpty(_config.AllowedExtensions))
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = _config.AllowedExtensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .ToList();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"File extension '{extension}' is not allowed. Allowed extensions: {_config.AllowedExtensions}");
        }
    }

    private string GetFileUrl(string key)
    {
        // Return CloudFront URL if configured, otherwise S3 URL
        if (!string.IsNullOrEmpty(_config.CloudFrontUrl))
        {
            return $"{_config.CloudFrontUrl.TrimEnd('/')}/{key}";
        }

        return $"https://{_config.BucketName}.s3.{_config.Region}.amazonaws.com/{key}";
    }

    private string? ExtractKeyFromUrl(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            
            // Check if it's a CloudFront URL
            if (!string.IsNullOrEmpty(_config.CloudFrontUrl) && 
                fileUrl.StartsWith(_config.CloudFrontUrl, StringComparison.OrdinalIgnoreCase))
            {
                return uri.AbsolutePath.TrimStart('/');
            }
            
            // Check if it's an S3 URL
            if (uri.Host.Contains($"{_config.BucketName}.s3") || 
                uri.Host.Contains("s3.amazonaws.com"))
            {
                return uri.AbsolutePath.TrimStart('/');
            }

            _logger.LogWarning("URL format not recognized: {Url}", fileUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting key from URL: {Url}", fileUrl);
            return null;
        }
    }

    #endregion
}
