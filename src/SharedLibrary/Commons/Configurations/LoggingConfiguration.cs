using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace SharedLibrary.Commons.Configurations;

public static class LoggingConfiguration
{
    // Categories to be filtered based on configuration
    private static readonly Dictionary<string, string> ConfigurableCategories = new()
    {
        ["Microsoft.EntityFrameworkCore.Database.Command"] = "LoggingConfig:EnableEfCommands",
        ["Microsoft.EntityFrameworkCore.Database"] = "LoggingConfig:EnableEfCommands",
        ["Microsoft.EntityFrameworkCore"] = "LoggingConfig:EnableEfCore", 
        ["Npgsql"] = "LoggingConfig:EnableNpgsql",
        ["Microsoft.Data.SqlClient"] = "LoggingConfig:EnableSqlClient",
        ["Microsoft.AspNetCore.StaticFiles"] = "LoggingConfig:EnableStaticFiles",
        ["Microsoft.AspNetCore.Hosting.Diagnostics"] = "LoggingConfig:EnableHostingDiagnostics",
        ["Microsoft.AspNetCore.Mvc.Infrastructure"] = "LoggingConfig:EnableMvcInfrastructure",
        ["Microsoft.AspNetCore.Routing"] = "LoggingConfig:EnableRouting",
        ["Microsoft.AspNetCore.Authentication"] = "LoggingConfig:EnableAuthentication",
        ["Microsoft.AspNetCore.Authorization"] = "LoggingConfig:EnableAuthorization",
        ["Ocelot"] = "LoggingConfig:EnableOcelot",
        ["MassTransit"] = "LoggingConfig:EnableMassTransit"
    };
    
    public static WebApplicationBuilder AddLoggingConfiguration(this WebApplicationBuilder builder, 
        string? serviceName = null)
    {
        var configuration = builder.Configuration;
        
        // Get logging configuration from environment
        var enableFileLogging = configuration.GetValue<bool>("LoggingConfig:EnableFileLogging", true);
        var enableConsoleLogging = configuration.GetValue<bool>("LoggingConfig:EnableConsoleLogging", true);
        var enableTracing = configuration.GetValue<bool>("LoggingConfig:EnableDistributedTracing", true);
        var logLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:MinimumLevel", "Information"));
        
        // Docker-specific logging controls from environment
        var dockerReduceEfCoreNoise = configuration.GetValue<bool>("LoggingConfig:DockerReduceEfCoreNoise", true);
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        
        var logFileName = string.IsNullOrEmpty(serviceName) 
            ? "microservice-{Date}.txt" 
            : $"{serviceName.ToLower()}-{{Date}}.txt";
            
        // Configure logging providers
        builder.Logging.ClearProviders();
        
        // Apply log filtering FIRST - before adding providers
        ApplyLogFiltering(builder.Logging, configuration);
        
        if (enableConsoleLogging)
        {
            // Simple console logging for both local and Docker
            builder.Logging.AddConsole();
        }
        
        // Create filter dictionary based on environment configuration
        var filterDictionary = CreateFilterDictionary(configuration);
        
        if (enableFileLogging)
        {
            builder.Logging.AddFile(
                $"Logs/{logFileName}",
                logLevel,
                filterDictionary,
                fileSizeLimitBytes: configuration.GetValue<int>("LoggingConfig:FileSizeLimitMB", 10) * 1024 * 1024,
                retainedFileCountLimit: configuration.GetValue<int>("LoggingConfig:RetainedFileCount", 30));
        }
        
        // Add distributed tracing if enabled
        if (enableTracing)
        {
            builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
        }
        
        return builder;
    }
    
    public static ILogger CreateStartupLogger(string? serviceName = null)
    {
        var logFileName = string.IsNullOrEmpty(serviceName) 
            ? "microservice-{Date}.txt" 
            : $"{serviceName.ToLower()}-{{Date}}.txt";
            
        // Load environment variables for startup logger
        var defaultFilterDictionary = new Dictionary<string, LogLevel>
        {
            ["Microsoft.EntityFrameworkCore.Database.Command"] = LogLevel.None,
            ["Microsoft.EntityFrameworkCore"] = LogLevel.Warning,
            ["Npgsql"] = LogLevel.Warning,
            ["Microsoft.Data.SqlClient"] = LogLevel.Warning,
            ["Microsoft.AspNetCore.StaticFiles"] = LogLevel.None,
            ["Microsoft.AspNetCore.Hosting.Diagnostics"] = LogLevel.None,
            ["Microsoft.AspNetCore.Mvc.Infrastructure"] = LogLevel.None
        };
        
        return LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.AddFile(
                $"Logs/{logFileName}",
                LogLevel.Information,
                defaultFilterDictionary,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 30);
                
            builder.AddFilter((category, level) => 
            {
                if (defaultFilterDictionary.TryGetValue(category, out var configuredLevel))
                {
                    return level >= configuredLevel && configuredLevel != LogLevel.None;
                }
                return level >= LogLevel.Information;
            });
        }).CreateLogger(serviceName ?? "MicroserviceProgram");
    }
    
    private static Dictionary<string, LogLevel> CreateFilterDictionary(IConfiguration configuration)
    {
        var filterDictionary = new Dictionary<string, LogLevel>();
        
        foreach (var kvp in ConfigurableCategories)
        {
            var categoryName = kvp.Key;
            var configKey = kvp.Value;
            var isEnabled = configuration.GetValue<bool>(configKey, false);
            
            filterDictionary[categoryName] = isEnabled ? LogLevel.Trace : LogLevel.None;
        }
        
        return filterDictionary;
    }
    
    private static void ApplyLogFiltering(ILoggingBuilder logging, IConfiguration configuration)
    {
        var minLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:MinimumLevel", "Information"));
        var dockerReduceEfCoreNoise = configuration.GetValue<bool>("LoggingConfig:DockerReduceEfCoreNoise", true);
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        
        // Set specific log levels for EF Core categories from environment
        var efCoreLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:EfCoreLevel", "None"));
        var efDatabaseCommandLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:EfDatabaseCommandLevel", "None"));
        var npgsqlLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:NpgsqlLevel", "None"));
        var sqlClientLevel = Enum.Parse<LogLevel>(configuration.GetValue<string>("LoggingConfig:SqlClientLevel", "None"));
        
        logging.AddFilter("Microsoft.EntityFrameworkCore", efCoreLevel);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", efDatabaseCommandLevel);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database", efDatabaseCommandLevel);
        logging.AddFilter("Npgsql", npgsqlLevel);
        logging.AddFilter("Microsoft.Data.SqlClient", sqlClientLevel);
        
        logging.AddFilter((category, level) => 
        {
            if (string.IsNullOrEmpty(category))
                return level >= minLevel;
                
            // In Docker with noise reduction, be more aggressive with EF Core filtering
            if (isDocker && dockerReduceEfCoreNoise)
            {
                if (category.StartsWith("Microsoft.EntityFrameworkCore") || 
                    category.StartsWith("Npgsql") || 
                    category.StartsWith("Microsoft.Data.SqlClient"))
                {
                    return false; // Completely disable EF Core logs in Docker console
                }
            }
                
            // Check if category should be filtered based on configuration
            foreach (var kvp in ConfigurableCategories)
            {
                if (category.StartsWith(kvp.Key))
                {
                    var isEnabled = configuration.GetValue<bool>(kvp.Value, false);
                    if (!isEnabled)
                        return false;
                    break;
                }
            }
            
            return level >= minLevel;
        });
    }
}

// Correlation ID Service for distributed tracing
public interface ICorrelationIdService
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}

public class CorrelationIdService : ICorrelationIdService
{
    private string _correlationId = Guid.NewGuid().ToString();
    
    public string CorrelationId => _correlationId;
    
    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId ?? Guid.NewGuid().ToString();
    }
}
