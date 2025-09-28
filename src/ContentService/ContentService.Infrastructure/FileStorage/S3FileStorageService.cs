using Amazon.S3;
using Amazon.S3.Model;
using ContentService.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContentService.Infrastructure.FileStorage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly string _bucketName;
    private readonly string _cloudFrontUrl;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _logger = logger;
        _bucketName = _configuration["AWS:S3:BucketName"] ?? 
            throw new InvalidOperationException("S3 bucket name not configured");
        _cloudFrontUrl = _configuration["AWS:CloudFront:DistributionUrl"] ?? "";
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? folderPath = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            // Generate unique file key
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = string.IsNullOrEmpty(folderPath) 
                ? uniqueFileName 
                : $"{folderPath.TrimEnd('/')}/{uniqueFileName}";

            _logger.LogInformation("Uploading file to S3: {Key}", key);

            // Create upload request
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead, // Make file publicly accessible
                Metadata =
                {
                    ["original-filename"] = file.FileName,
                    ["upload-timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            // Upload file
            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Return CloudFront URL if configured, otherwise S3 URL
                var fileUrl = !string.IsNullOrEmpty(_cloudFrontUrl)
                    ? $"{_cloudFrontUrl.TrimEnd('/')}/{key}"
                    : $"https://{_bucketName}.s3.amazonaws.com/{key}";

                _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);
                return fileUrl;
            }

            throw new InvalidOperationException($"Failed to upload file. HTTP Status: {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            throw;
        }
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
                BucketName = _bucketName,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, TimeSpan expirationTime)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.Add(expirationTime),
                Verb = HttpVerb.GET
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            _logger.LogInformation("Generated presigned URL for key: {Key}", fileKey);
            
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
                BucketName = _bucketName,
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

    private string? ExtractKeyFromUrl(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            
            // Handle CloudFront URLs
            if (!string.IsNullOrEmpty(_cloudFrontUrl) && fileUrl.StartsWith(_cloudFrontUrl))
            {
                return fileUrl.Substring(_cloudFrontUrl.TrimEnd('/').Length + 1);
            }
            
            // Handle S3 URLs
            if (uri.Host.Contains("s3.amazonaws.com") || uri.Host.EndsWith(".s3.amazonaws.com"))
            {
                return uri.AbsolutePath.TrimStart('/');
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}