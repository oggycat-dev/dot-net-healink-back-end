using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using ProductAuthMicroservice.Commons.Behaviors;

namespace ProductAuthMicroservice.Commons.DependencyInjection;

/// <summary>
/// MediatR configuration extensions for microservices
/// </summary>
public static class MediatRExtensions
{
    /// <summary>
    /// Add MediatR with shared behaviors
    /// </summary>
    public static IServiceCollection AddMediatRWithBehaviors(this IServiceCollection services, 
        Assembly assembly)
    {
        // Register MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Add shared pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });
        
        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);
        
        return services;
    }
    
    /// <summary>
    /// Add MediatR with custom behaviors
    /// </summary>
    public static IServiceCollection AddMediatRWithCustomBehaviors(this IServiceCollection services,
        Assembly assembly,
        params Type[] additionalBehaviors)
    {
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Add shared pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            
            // Add additional behaviors
            foreach (var behaviorType in additionalBehaviors)
            {
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), behaviorType);
            }
        });
        
        services.AddValidatorsFromAssembly(assembly);
        
        return services;
    }
}
