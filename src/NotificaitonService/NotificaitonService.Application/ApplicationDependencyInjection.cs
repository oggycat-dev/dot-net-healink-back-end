using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services here if needed
        
        return services;
    }
}