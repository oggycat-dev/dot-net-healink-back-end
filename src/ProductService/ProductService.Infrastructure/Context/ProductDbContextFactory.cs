using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.Commons.Factories;

namespace ProductService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating ProductDbContext instances
/// Used by EF Core tools for migrations and design-time operations
/// </summary>
public class ProductContextFactory : BaseDbContextFactory<ProductDbContext>
{
    /// <summary>
    /// Creates ProductDbContext instance with the provided options
    /// </summary>
    protected override ProductDbContext CreateContext(DbContextOptions<ProductDbContext> options)
    {
        return new ProductDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from ProductService.Infrastructure to ProductService.API
    /// This allows the factory to find appsettings.json in the API project
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../ProductService.API";
    }
}