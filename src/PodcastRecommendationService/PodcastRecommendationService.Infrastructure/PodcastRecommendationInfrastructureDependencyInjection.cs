using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PodcastRecommendationService.Application.Services;
using PodcastRecommendationService.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;

using System.Net;

namespace PodcastRecommendationService.Infrastructure;

public static class PodcastRecommendationInfrastructureDependencyInjection
{
    public static IServiceCollection AddPodcastRecommendationInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure HttpClient for UserService communication
        services.AddHttpClient("UserService", client =>
        {
            // Đọc từ environment variable
            var userServiceUrl = Environment.GetEnvironmentVariable("RECOMMENDATION_USER_SERVICE_URL") 
                ?? configuration["USER_SERVICE_URL"]
                ?? "http://userservice-api";
            client.BaseAddress = new Uri(userServiceUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "PodcastRecommendationService/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
                
        // Configure HttpClient for ContentService communication  
        services.AddHttpClient("ContentService", client =>
        {
            // Đọc từ environment variable
            var contentServiceUrl = Environment.GetEnvironmentVariable("RECOMMENDATION_CONTENT_SERVICE_URL")
                ?? configuration["CONTENT_SERVICE_URL"]
                ?? "http://contentservice-api";
            client.BaseAddress = new Uri(contentServiceUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "PodcastRecommendationService/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configure HTTP client for data fetching from other microservices
        services.AddHttpClient("DataFetchClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("User-Agent", "PodcastRecommendationService-DataFetch/1.0");
        });

        // Configure HttpClient for FastAPI AI Service
        services.AddHttpClient<FastAPIRecommendationService>(client =>
        {
            // Đọc từ environment variable
            var aiServiceUrl = Environment.GetEnvironmentVariable("RECOMMENDATION_AI_SERVICE_BASE_URL")
                ?? configuration["PODCAST_AI_SERVICE_URL"]
                ?? "http://podcast-ai-service:8000";
            var timeoutSeconds = int.TryParse(
                Environment.GetEnvironmentVariable("RECOMMENDATION_AI_SERVICE_TIMEOUT_SECONDS"), 
                out var timeout) ? timeout : 30;
            
            client.BaseAddress = new Uri(aiServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "PodcastRecommendationService/2.0");
        });

        // Register services
        services.AddScoped<IDataFetchService, DataFetchService>();
        services.AddScoped<IRecommendationService, FastAPIRecommendationService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, timespan) =>
                {
                    // Log circuit breaker opened
                },
                onReset: () =>
                {
                    // Log circuit breaker closed
                });
    }
}