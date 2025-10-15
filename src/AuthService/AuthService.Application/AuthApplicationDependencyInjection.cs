using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.DependencyInjection;

namespace AuthService.Application;

public static class AuthApplicationDependencyInjection
{
    /// <summary>
    /// Add application services for AuthService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        //To do: Add Event Handlers

        return services;
    }
}
