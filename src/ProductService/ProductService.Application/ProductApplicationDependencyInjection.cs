using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProductAuthMicroservice.Commons.DependencyInjection;

namespace ProductService.Application;

public static class ProductApplicationDependencyInjection
{
    /// <summary>
    /// Add application services for ProductService
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR with shared behaviors
        services.AddMediatRWithBehaviors(Assembly.GetExecutingAssembly());
        
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Add product-specific application services here
        // services.AddScoped<IProductService, ProductService>();
        
        return services;
    }
}
