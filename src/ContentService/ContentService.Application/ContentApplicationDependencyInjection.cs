using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using MediatR;
using ContentService.Application.Behaviors;

namespace ContentService.Application;

public static class ContentApplicationDependencyInjection
{
    public static IServiceCollection AddContentApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        services.AddAutoMapper(cfg => 
        {
            cfg.AddMaps(typeof(ContentApplicationDependencyInjection).Assembly);
        });
        
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add FluentValidation behavior for MediatR
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
}