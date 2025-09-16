using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ProductAuthMicroservice.Commons.Configurations;

public static class LoggingConfiguration
{
    // Categories to be completely filtered out from log files
    private static readonly string[] ExcludedCategories = new[] 
    {
        "Microsoft.AspNetCore.StaticFiles",
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "Microsoft.AspNetCore.Mvc.Infrastructure"
    };
    
    public static WebApplicationBuilder AddLoggingConfiguration(this WebApplicationBuilder builder, 
        string? serviceName = null)
    {
        var logFileName = string.IsNullOrEmpty(serviceName) 
            ? "microservice-{Date}.txt" 
            : $"{serviceName.ToLower()}-{{Date}}.txt";
            
        // Configure logging to filter out unnecessary logs
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        // Create a dictionary with category-specific log levels
        var filterDictionary = new Dictionary<string, LogLevel>
        {
            // Completely suppress all database command logs
            ["Microsoft.EntityFrameworkCore.Database.Command"] = LogLevel.None,
            // Set higher threshold for other EF messages
            ["Microsoft.EntityFrameworkCore"] = LogLevel.Warning,
            ["Npgsql"] = LogLevel.Warning,
            ["Microsoft.Data.SqlClient"] = LogLevel.Warning
        };
        
        // Add completely excluded categories
        foreach (var category in ExcludedCategories)
        {
            filterDictionary[category] = LogLevel.None;
        }
        
        // Add file logging with custom filter dictionary
        builder.Logging.AddFile(
            $"Logs/{logFileName}",
            LogLevel.Information,
            filterDictionary,
            fileSizeLimitBytes: 10 * 1024 * 1024,  // 10 MB size limit
            retainedFileCountLimit: 30);  // Keep logs for 30 days
            
        // Add additional event filter
        builder.Logging.AddFilter((category, level) => 
        {
            // If it's an EF Core DB Command log, filter it out completely
            if (category == "Microsoft.EntityFrameworkCore.Database.Command")
            {
                return false;
            }
            
            // Filter out static file requests in production
            if (category?.Contains("StaticFiles") == true && level < LogLevel.Warning)
            {
                return false;
            }
            
            // For all other categories, use standard filtering
            return level >= LogLevel.Information;
        });
        
        return builder;
    }
    
    public static ILogger CreateStartupLogger(string? serviceName = null)
    {
        var logFileName = string.IsNullOrEmpty(serviceName) 
            ? "microservice-{Date}.txt" 
            : $"{serviceName.ToLower()}-{{Date}}.txt";
            
        // Create a dictionary with category-specific log levels
        var filterDictionary = new Dictionary<string, LogLevel>
        {
            // Completely suppress all database command logs
            ["Microsoft.EntityFrameworkCore.Database.Command"] = LogLevel.None,
            // Set higher threshold for other EF messages
            ["Microsoft.EntityFrameworkCore"] = LogLevel.Warning,
            ["Npgsql"] = LogLevel.Warning,
            ["Microsoft.Data.SqlClient"] = LogLevel.Warning
        };
        
        // Add completely excluded categories
        foreach (var category in ExcludedCategories)
        {
            filterDictionary[category] = LogLevel.None;
        }
        
        return LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            
            // Use the filter dictionary with the file provider
            builder.AddFile(
                $"Logs/{logFileName}",
                LogLevel.Information,
                filterDictionary,
                fileSizeLimitBytes: 10 * 1024 * 1024,  // 10 MB size limit
                retainedFileCountLimit: 30);  // Keep logs for 30 days
                
            // Add additional event filter
            builder.AddFilter((category, level) => 
            {
                // If it's an EF Core DB Command log, filter it out completely
                if (category == "Microsoft.EntityFrameworkCore.Database.Command")
                {
                    return false;
                }
                
                // For all other categories, use standard filtering
                return level >= LogLevel.Information;
            });
        }).CreateLogger(serviceName ?? "MicroserviceProgram");
    }
}
