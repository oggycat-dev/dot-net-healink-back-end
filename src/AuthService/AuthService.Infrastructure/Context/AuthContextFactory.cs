using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.Commons.Factories;

namespace AuthService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating AuthDbContext instances
/// Used by EF Core tools for migrations and design-time operations
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
    /// This allows the factory to find appsettings.json in the API project
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../AuthService.API";
    }
}