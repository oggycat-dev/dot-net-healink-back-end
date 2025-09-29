using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace ContentService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating ContentDbContext instances
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
public class ContentDbContextFactory : BaseDbContextFactory<ContentDbContext>
{
    /// <summary>
    /// Creates ContentDbContext instance with the provided options
    /// </summary>
    protected override ContentDbContext CreateContext(DbContextOptions<ContentDbContext> options)
    {
        return new ContentDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from ContentService.Infrastructure to ContentService.API
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../ContentService.API";
    }

    /// <summary>
    /// Gets the connection string environment variable name for ContentService
    /// </summary>
    protected override string GetConnectionStringEnvironmentVariable()
    {
        return "CONTENT_DB_CONNECTION_STRING";
    }
}
