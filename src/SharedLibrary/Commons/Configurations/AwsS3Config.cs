namespace SharedLibrary.Commons.Configurations;

/// <summary>
/// AWS S3 configuration settings
/// </summary>
public class AwsS3Config
{
    public const string SectionName = "AwsS3Config";
    
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;
    
    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// AWS Region (e.g., us-east-1, ap-southeast-1)
    /// </summary>
    public string Region { get; set; } = "ap-southeast-1";
    
    /// <summary>
    /// S3 Bucket Name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;
    
    /// <summary>
    /// CloudFront Distribution URL (optional, for CDN)
    /// </summary>
    public string? CloudFrontUrl { get; set; }
    
    /// <summary>
    /// Enable server-side encryption
    /// </summary>
    public bool EnableEncryption { get; set; } = true;
    
    /// <summary>
    /// Default ACL for uploaded files (public-read, private, etc.)
    /// </summary>
    public string DefaultAcl { get; set; } = "public-read";
    
    /// <summary>
    /// Maximum file size in bytes (default 100MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;
    
    /// <summary>
    /// Allowed file extensions (comma-separated, e.g., ".jpg,.png,.pdf")
    /// </summary>
    public string? AllowedExtensions { get; set; }
    
    /// <summary>
    /// Validate configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccessKey))
            throw new InvalidOperationException("AWS Access Key is required");
            
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("AWS Secret Key is required");
            
        if (string.IsNullOrWhiteSpace(Region))
            throw new InvalidOperationException("AWS Region is required");
            
        if (string.IsNullOrWhiteSpace(BucketName))
            throw new InvalidOperationException("S3 Bucket Name is required");
    }
}
