using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Commons.DependencyInjection;

namespace UserService.Application;

public static class UserApplicationDependencyInjection
{
    
    /// <summary>
    /// Add application services for UserService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
