using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.Configurations;
using SharedLibrary.Commons.FileStorage;
using SharedLibrary.Commons.Interfaces;

namespace SharedLibrary.Commons.DependencyInjection;

/// <summary>
/// Extension methods for adding file storage services to the service collection
/// </summary>
public static class FileStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add AWS S3 file storage service
    /// </summary>
    public static IServiceCollection AddS3FileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register AWS S3 configuration
        services.Configure<AwsS3Config>(configuration.GetSection(AwsS3Config.SectionName));

        // Get AWS credentials from configuration
        var awsConfig = configuration.GetSection(AwsS3Config.SectionName).Get<AwsS3Config>();
        
        if (awsConfig == null)
        {
            throw new InvalidOperationException(
                $"AWS S3 configuration section '{AwsS3Config.SectionName}' not found in configuration");
        }

        // Validate configuration
        awsConfig.Validate();

        // Register AWS S3 client with credentials
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var credentials = new BasicAWSCredentials(awsConfig.AccessKey, awsConfig.SecretKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region),
                ForcePathStyle = false,
                UseHttp = false
            };
            
            return new AmazonS3Client(credentials, config);
        });

        // Register file storage service
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        return services;
    }

    /// <summary>
    /// Add AWS S3 file storage service with explicit credentials
    /// </summary>
    public static IServiceCollection AddS3FileStorage(
        this IServiceCollection services,
        string accessKey,
        string secretKey,
        string region,
        string bucketName,
        string? cloudFrontUrl = null)
    {
        // Register AWS S3 configuration
        services.Configure<AwsS3Config>(config =>
        {
            config.AccessKey = accessKey;
            config.SecretKey = secretKey;
            config.Region = region;
            config.BucketName = bucketName;
            config.CloudFrontUrl = cloudFrontUrl;
        });

        // Register AWS S3 client with credentials
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region),
                ForcePathStyle = false,
                UseHttp = false
            };
            
            return new AmazonS3Client(credentials, config);
        });

        // Register file storage service
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        return services;
    }
}
