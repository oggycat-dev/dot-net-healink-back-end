using Microsoft.Extensions.DependencyInjection;
using PodcastRecommendationService.Application.Services;
using FluentValidation;
using System.Reflection;

namespace PodcastRecommendationService.Application;

public static class PodcastRecommendationApplicationDependencyInjection
{
    public static IServiceCollection AddPodcastRecommendationApplication(this IServiceCollection services)
    {
        // Add MediatR for CQRS pattern
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}