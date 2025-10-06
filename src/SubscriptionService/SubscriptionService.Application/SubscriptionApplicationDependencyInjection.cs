using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using SharedLibrary.Commons.DependencyInjection;

namespace SubscriptionService.Application;

/// <summary>
/// Dependency injection configuration for Subscription Application layer
/// </summary>
public static class SubscriptionApplicationDependencyInjection
{
    /// <summary>
    /// Add Subscription Application services
    /// </summary>
    public static IServiceCollection AddSubscriptionApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        //TODO: Add Event Handlers

        return services;
    }
}
