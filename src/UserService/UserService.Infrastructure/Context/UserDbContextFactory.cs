using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace UserService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating UserDbContext instances
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
public class UserDbContextFactory : BaseDbContextFactory<UserDbContext>
{
    /// <summary>
    /// Creates UserDbContext instance with the provided options
    /// </summary>
    protected override UserDbContext CreateContext(DbContextOptions<UserDbContext> options)
    {
        return new UserDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from UserService.Infrastructure to UserService.API
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../UserService.API";
    }

    /// <summary>
    /// Gets the connection string environment variable name for UserService
    /// </summary>
    protected override string GetConnectionStringEnvironmentVariable()
    {
        return "USER_DB_CONNECTION_STRING";
    }
}

