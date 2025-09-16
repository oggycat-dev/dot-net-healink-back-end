using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProductAuthMicroservice.Commons.Extensions;

/// <summary>
/// Generic extension methods for seeding utilities in microservices
/// Contains only general database seeding helpers, not business-specific logic
/// </summary>
public static class SeedingExtension
{
    /// <summary>
    /// Check if seeding is enabled in configuration
    /// </summary>
    public static bool IsSeedingEnabled(this IConfiguration configuration)
    {
        return configuration.GetSection("DataConfig").GetValue<bool>("EnableSeeding");
    }

    /// <summary>
    /// Get admin account configuration
    /// </summary>
    public static (string? Email, string? Password) GetAdminAccountConfig(this IConfiguration configuration)
    {
        var adminEmail = configuration.GetSection("DefaultAdminAccount").GetValue<string>("Email")?.Trim();
        var adminPassword = configuration.GetSection("DefaultAdminAccount").GetValue<string>("Password");
        return (adminEmail, adminPassword);
    }

    /// <summary>
    /// Log seeding start message
    /// </summary>
    public static void LogSeedingStart(this ILogger logger, string serviceName, string dataType)
    {
        logger.LogInformation("{ServiceName}: Starting {DataType} seeding...", serviceName, dataType);
    }

    /// <summary>
    /// Log seeding completion message
    /// </summary>
    public static void LogSeedingComplete(this ILogger logger, string serviceName, string dataType)
    {
        logger.LogInformation("{ServiceName}: {DataType} seeding completed.", serviceName, dataType);
    }

    /// <summary>
    /// Log seeding disabled message
    /// </summary>
    public static void LogSeedingDisabled(this ILogger logger, string serviceName)
    {
        logger.LogInformation("{ServiceName}: Data seeding is disabled.", serviceName);
    }

    /// <summary>
    /// Log missing configuration warning
    /// </summary>
    public static void LogMissingConfig(this ILogger logger, string serviceName, string configSection)
    {
        logger.LogWarning("{ServiceName}: {ConfigSection} configuration is missing. Skipping related seeding.", 
            serviceName, configSection);
    }

    /// <summary>
    /// Log successful entity creation
    /// </summary>
    public static void LogEntityCreated(this ILogger logger, string serviceName, string entityType, string identifier)
    {
        logger.LogInformation("{ServiceName}: Created {EntityType}: {Identifier}", serviceName, entityType, identifier);
    }

    /// <summary>
    /// Log entity already exists
    /// </summary>
    public static void LogEntityExists(this ILogger logger, string serviceName, string entityType, string identifier)
    {
        logger.LogInformation("{ServiceName}: {EntityType} {Identifier} already exists", serviceName, entityType, identifier);
    }

    /// <summary>
    /// Log entity creation failure
    /// </summary>
    public static void LogEntityCreationFailed(this ILogger logger, string serviceName, string entityType, string errors)
    {
        logger.LogWarning("{ServiceName}: Failed to create {EntityType}: {Errors}", serviceName, entityType, errors);
    }
}