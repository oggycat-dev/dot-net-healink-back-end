using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Factories;

namespace SubscriptionService.Infrastructure.Context;

/// <summary>
/// Design-time factory for creating SubscriptionDbContext instances
/// Simple pattern like Booklify - just load .env and get connection string
/// </summary>
public class SubscriptionDbContextFactory : BaseDbContextFactory<SubscriptionDbContext>
{
    /// <summary>
    /// Creates SubscriptionDbContext instance with the provided options
    /// </summary>
    protected override SubscriptionDbContext CreateContext(DbContextOptions<SubscriptionDbContext> options)
    {
        return new SubscriptionDbContext(options);
    }

    /// <summary>
    /// Gets the relative path from SubscriptionService.Infrastructure to SubscriptionService.API
    /// </summary>
    protected override string GetApiProjectRelativePath()
    {
        return "../Subscription.API";
    }

    /// <summary>
    /// Gets the connection string environment variable name for SubscriptionService
    /// </summary>
    protected override string GetConnectionStringEnvironmentVariable()
    {
        return "SUBSCRIPTION_DB_CONNECTION_STRING";
    }
}
