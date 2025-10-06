using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using SharedLibrary.Commons.DependencyInjection;

namespace PaymentService.Application;

/// <summary>
/// Dependency injection configuration for Payment Application layer
/// </summary>
public static class PaymentApplicationDependencyInjection
{
    /// <summary>
    /// Add Payment Application services
    /// </summary>
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        //TODO: Add Event Handlers

        return services;
    }
}


