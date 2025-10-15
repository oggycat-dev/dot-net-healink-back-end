using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace AuthService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating AuthDbContext instances
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
public class AuthContextFactory : BaseDbContextFactory<AuthDbContext>
{
    /// <summary>
    /// Creates AuthDbContext instance with the provided options
    /// </summary>
    protected override AuthDbContext CreateContext(DbContextOptions<AuthDbContext> options)
    {
        return new AuthDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from AuthService.Infrastructure to AuthService.API
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../AuthService.API";
    }

    /// <summary>
    /// Gets the connection string environment variable name for AuthService
    /// </summary>
    protected override string GetConnectionStringEnvironmentVariable()
    {
        return "AUTH_DB_CONNECTION_STRING";
    }
}