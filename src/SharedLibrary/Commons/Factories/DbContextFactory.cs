using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Commons.Configs;

namespace SharedLibrary.Commons.Factories;

/// <summary>
/// Base abstract factory for creating DbContext instances at design time
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
/// <typeparam name="TContext">The DbContext type to create</typeparam>
public abstract class BaseDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Gets the relative path from Infrastructure project to API project
    /// </summary>
    protected abstract string GetApiProjectRelativePath();

    /// <summary>
    /// Creates the specific DbContext instance
    /// </summary>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Gets the connection string environment variable name for this service
    /// </summary>
    protected abstract string GetConnectionStringEnvironmentVariable();

    public TContext CreateDbContext(string[] args)
    {
        // Load .env file (multiple locations like Booklify)
        var envPaths = new[]
        {
            ".env.development",                                        // Current directory
            ".env",                                                   // Current directory
            Path.Combine("..", ".env.development"),                  // Parent directory
            Path.Combine("..", ".env"),                              // Parent directory  
            Path.Combine("..", "..", ".env.development"),            // Project root from src subfolder
            Path.Combine("..", "..", ".env"),                        // Project root from src subfolder
            Path.Combine("..", "..", "..", ".env.development"),      // Project root from deeper structure
            Path.Combine("..", "..", "..", ".env")                   // Project root from deeper structure
        };

        foreach (var envPath in envPaths)
        {
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
                break;
            }
        }

        // Get connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable(GetConnectionStringEnvironmentVariable());
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string not found. Please set the {GetConnectionStringEnvironmentVariable()} environment variable in .env file");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        
        // Configure PostgreSQL (simple setup)
        optionsBuilder.UseNpgsql(connectionString, 
            npgsqlOptions => 
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                    
                npgsqlOptions.MigrationsAssembly(typeof(TContext).Assembly.FullName);
            });

        return CreateContext(optionsBuilder.Options);
    }

}