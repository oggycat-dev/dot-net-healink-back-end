using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace UserService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating UserDbContext instances
/// Used by EF Core tools for migrations and design-time operations
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
    /// This allows the factory to find appsettings.json in the API project
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../UserService.API";
    }
}

