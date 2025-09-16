using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ProductAuthMicroservice.Commons.Configs;

namespace ProductAuthMicroservice.Commons.Factories;

/// <summary>
/// Base abstract factory for creating DbContext instances at design time
/// Provides common configuration loading and PostgreSQL setup
/// </summary>
/// <typeparam name="TContext">The DbContext type to create</typeparam>
public abstract class BaseDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Gets the relative path from Infrastructure project to API project
    /// Override this in derived classes for different project structures
    /// </summary>
    protected abstract string GetApiProjectRelativePath();

    /// <summary>
    /// Creates the specific DbContext instance
    /// Override this in derived classes to instantiate the correct context type
    /// </summary>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Gets additional configuration files to load (beyond appsettings.json)
    /// Override to add service-specific config files
    /// </summary>
    protected virtual string[] GetAdditionalConfigFiles() => Array.Empty<string>();

    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        
        // Build configuration from API project
        var configuration = BuildConfiguration();
        
        // Get connection configuration
        var connectionConfig = configuration.GetSection("ConnectionConfig").Get<ConnectionConfig>() 
                              ?? new ConnectionConfig();
        
        var connectionString = connectionConfig.DefaultConnection;
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' not found. Make sure appsettings.json exists in {GetApiProjectRelativePath()} project.");
        }

        // Configure PostgreSQL with retry policy
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                if (connectionConfig.RetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: connectionConfig.MaxRetryCount > 0 ? connectionConfig.MaxRetryCount : 3,
                        maxRetryDelay: TimeSpan.FromSeconds(connectionConfig.MaxRetryDelay > 0 ? connectionConfig.MaxRetryDelay : 30),
                        errorCodesToAdd: connectionConfig.ErrorNumbersToAdd);
                }
                
                // Set migrations assembly to the context's assembly
                npgsqlOptions.MigrationsAssembly(typeof(TContext).Assembly.FullName);
            });

        // Enable sensitive data logging in development
        if (IsDevelopmentEnvironment(args))
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        return CreateContext(optionsBuilder.Options);
    }

    private IConfigurationRoot BuildConfiguration()
    {
        // Get API project path
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), GetApiProjectRelativePath());
        
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

        // Add additional config files if specified
        foreach (var configFile in GetAdditionalConfigFiles())
        {
            configBuilder.AddJsonFile(configFile, optional: true, reloadOnChange: false);
        }

        return configBuilder.Build();
    }

    private static bool IsDevelopmentEnvironment(string[] args)
    {
        return args.Contains("--environment") && 
               args.SkipWhile(arg => arg != "--environment").Skip(1).FirstOrDefault() == "Development" ||
               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }
}