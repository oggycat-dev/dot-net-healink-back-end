using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace PaymentService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating PaymentDbContext instances
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
public class PaymentDbContextFactory : BaseDbContextFactory<PaymentDbContext>
{
    /// <summary>
    /// Creates PaymentDbContext instance with the provided options
    /// </summary>
    protected override PaymentDbContext CreateContext(DbContextOptions<PaymentDbContext> options)
    {
        return new PaymentDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from PaymentService.Infrastructure to PaymentService.API
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../PaymentService.API";
    }

    /// <summary>
    /// Gets the connection string environment variable name for PaymentService
    /// </summary>
    protected override string GetConnectionStringEnvironmentVariable()
    {
        return "PAYMENT_DB_CONNECTION_STRING";
    }
}