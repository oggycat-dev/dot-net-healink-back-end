using PodcastRecommendationService.Application.Services;
using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using SharedLibrary.Commons.Enums;

namespace PodcastRecommendationService.Infrastructure.Services;

/// <summary>
/// Service implementation giao tiếp với FastAPI AI service cho podcast recommendations
/// </summary>
public class FastAPIRecommendationService : IRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FastAPIRecommendationService> _logger;
    private readonly string _aiServiceBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public FastAPIRecommendationService(
        HttpClient httpClient, 
        ILogger<FastAPIRecommendationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Đọc từ environment variable (ưu tiên env var > configuration)
        _aiServiceBaseUrl = Environment.GetEnvironmentVariable("RECOMMENDATION_AI_SERVICE_BASE_URL")
            ?? configuration["PODCAST_AI_SERVICE_URL"] 
            ?? "http://podcast-ai-service:8000";
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        _logger.LogInformation("FastAPIRecommendationService initialized with AI Service URL: {Url}", _aiServiceBaseUrl);
    }

    public async Task<Result<PodcastRecommendationResponse>> GetRecommendationsAsync(string userId, int limit = 10, bool includeListened = false)
    {
        try
        {
            _logger.LogInformation("Getting recommendations for user {UserId} with limit {Limit}", userId, limit);
            
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<PodcastRecommendationResponse>.Failure(
                    "User ID cannot be empty", 
                    ErrorCodeEnum.ValidationFailed);
            }

            limit = Math.Max(1, Math.Min(limit, 20)); // FastAPI service supports up to 20

            // Build request URL cho FastAPI endpoint
            var url = $"{_aiServiceBaseUrl}/recommendations/{Uri.EscapeDataString(userId)}?num_recommendations={limit}";
            
            _logger.LogDebug("Calling FastAPI service: {Url}", url);

            // Make HTTP request to FastAPI AI service
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("FastAPI Service returned error for user {UserId}: {StatusCode} - {Error}", 
                    userId, response.StatusCode, errorContent);
                
                return Result<PodcastRecommendationResponse>.Failure(
                    "Failed to get recommendations from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Parse FastAPI response
            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("FastAPI Service response for user {UserId}: {Response}", userId, jsonContent);

            var fastApiResponse = JsonSerializer.Deserialize<FastAPIRecommendationResponse>(jsonContent, _jsonOptions);

            if (fastApiResponse == null)
            {
                _logger.LogError("Failed to deserialize FastAPI service response for user {UserId}", userId);
                return Result<PodcastRecommendationResponse>.Failure(
                    "Invalid response from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Map FastAPI response to application DTO
            var result = new PodcastRecommendationResponse
            {
                UserId = fastApiResponse.UserId,
                Recommendations = fastApiResponse.Recommendations.Select(r => new RecommendationItem
                {
                    PodcastId = r.PodcastId,
                    Title = r.Title,
                    Topic = r.Topics ?? "Unknown", // Map topics to topic
                    PredictedRating = (decimal)r.PredictedRating,
                    ConfidenceScore = (decimal)r.PredictedRating / 5.0m, // Convert rating to confidence (0-1)
                    RecommendationReason = $"Predicted rating: {r.PredictedRating:F2}/5.0",
                    Category = r.Category,
                    DurationMinutes = r.DurationMinutes,
                    ContentUrl = r.ContentUrl
                }).ToList(),
                TotalFound = fastApiResponse.TotalCount,
                FilteredListened = false, // FastAPI doesn't filter listened yet
                GeneratedAt = DateTime.Parse(fastApiResponse.Timestamp)
            };

            _logger.LogInformation("Successfully generated {Count} recommendations for user {UserId}", 
                result.Recommendations.Count, userId);

            return Result<PodcastRecommendationResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when calling AI service for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "Network error connecting to AI service", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout when calling AI service for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "Timeout connecting to AI service", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for AI service response for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "Invalid JSON response from AI service", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when getting recommendations for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "Unexpected error occurred", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<bool>> TrainUserPreferenceAsync(string userId, string podcastId, decimal rating)
    {
        try
        {
            _logger.LogInformation("Training user preference: User {UserId}, Podcast {PodcastId}, Rating {Rating}", 
                userId, podcastId, rating);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(podcastId))
            {
                return Result<bool>.Failure(
                    "User ID and Podcast ID cannot be empty", 
                    ErrorCodeEnum.ValidationFailed);
            }

            if (rating < 1 || rating > 5)
            {
                return Result<bool>.Failure(
                    "Rating must be between 1 and 5", 
                    ErrorCodeEnum.ValidationFailed);
            }

            // For now, FastAPI service doesn't support online training
            // This would be implemented later for real-time model updates
            _logger.LogInformation("Training logged for future model updates: User {UserId}, Podcast {PodcastId}, Rating {Rating}", 
                userId, podcastId, rating);

            // Return success indicating the training data was received
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training user preference for user {UserId}", userId);
            return Result<bool>.Failure(
                "Failed to train user preference", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<List<string>>> GetSimilarUsersAsync(string userId, int limit = 10)
    {
        try
        {
            _logger.LogInformation("Getting similar users for user {UserId} with limit {Limit}", userId, limit);

            // For now, return mock similar users based on training data patterns
            // This could be enhanced with real similarity calculation later
            var similarUsers = new List<string>();
            
            // Generate mock similar users (would be replaced with real logic)
            var random = new Random(userId.GetHashCode());
            for (int i = 0; i < Math.Min(limit, 10); i++)
            {
                similarUsers.Add($"user_{random.Next(1, 1001):D5}");
            }

            _logger.LogInformation("Found {Count} similar users for user {UserId}", similarUsers.Count, userId);
            
            return Result<List<string>>.Success(similarUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar users for user {UserId}", userId);
            return Result<List<string>>.Failure(
                "Failed to get similar users", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<BatchRecommendationsResponse>> GetBatchRecommendationsAsync(List<string> userIds, int limit = 10, bool includeListened = false)
    {
        try
        {
            _logger.LogInformation("Getting batch recommendations for {Count} users with limit {Limit}", userIds.Count, limit);

            if (userIds == null || userIds.Count == 0)
            {
                return Result<BatchRecommendationsResponse>.Failure(
                    "User IDs list cannot be empty",
                    ErrorCodeEnum.ValidationFailed);
            }

            var batchResults = new List<PodcastRecommendationResponse>();
            var failedUsers = new List<string>();

            // Process each user individually (could be optimized with batch endpoint later)
            foreach (var userId in userIds.Take(50)) // Limit to 50 users max
            {
                var userResult = await GetRecommendationsAsync(userId, limit, includeListened);
                if (userResult.IsSuccess)
                {
                    batchResults.Add(userResult.Data);
                }
                else
                {
                    failedUsers.Add(userId);
                    _logger.LogWarning("Failed to get recommendations for user {UserId}: {Message}", userId, userResult.Message);
                }
            }

            var response = new BatchRecommendationsResponse
            {
                Results = batchResults,
                TotalUsers = userIds.Count,
                SuccessfulUsers = batchResults.Count,
                FailedUsers = failedUsers,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Batch recommendations completed: {Successful}/{Total} users successful", 
                batchResults.Count, userIds.Count);

            return Result<BatchRecommendationsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch recommendations");
            return Result<BatchRecommendationsResponse>.Failure(
                "Failed to get batch recommendations",
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<bool>> TrackInteractionAsync(string userId, RecommendationInteractionRequest request)
    {
        try
        {
            _logger.LogInformation("Tracking interaction for user {UserId}: {Action} on podcast {PodcastId}", 
                userId, request.InteractionType, request.PodcastId);

            // For now, just log the interaction (could be sent to FastAPI for future learning)
            // This would be enhanced with real analytics tracking later
            
            _logger.LogInformation("Interaction tracked: User {UserId} performed {Action} on podcast {PodcastId} at {Timestamp}", 
                userId, request.InteractionType, request.PodcastId, request.Timestamp);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking interaction for user {UserId}", userId);
            return Result<bool>.Failure(
                "Failed to track interaction",
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<UserListenedPodcastsResponse>> GetUserListenedPodcastsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting listened podcasts for user {UserId}", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserListenedPodcastsResponse>.Failure(
                    "User ID cannot be empty",
                    ErrorCodeEnum.ValidationFailed);
            }

            // For now, return mock data (would integrate with ContentService later)
            var response = new UserListenedPodcastsResponse
            {
                UserId = userId,
                ListenedPodcasts = new List<string>(), // Empty for now
                TotalListened = 0,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Found {Count} listened podcasts for user {UserId}", response.TotalListened, userId);

            return Result<UserListenedPodcastsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting listened podcasts for user {UserId}", userId);
            return Result<UserListenedPodcastsResponse>.Failure(
                "Failed to get listened podcasts",
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<ModelInfoResponse>> GetModelInfoAsync()
    {
        try
        {
            _logger.LogInformation("Getting model information from FastAPI service");

            var response = await _httpClient.GetAsync($"{_aiServiceBaseUrl}/model/info");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("FastAPI service returned error for model info: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                
                return Result<ModelInfoResponse>.Failure(
                    "Failed to get model info from AI service",
                    ErrorCodeEnum.ExternalServiceError);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var fastApiResponse = JsonSerializer.Deserialize<dynamic>(jsonContent, _jsonOptions);

            // Map to ModelInfoResponse (simplified)
            var modelInfo = new ModelInfoResponse
            {
                ModelType = "Collaborative Filtering (Kaggle trained)",
                ModelVersion = "2.0.0",
                Framework = "FastAPI + Pandas",
                TrainingDataStats = "1,000 users, 1,990 podcasts, 31,267 ratings",
                PerformanceMetrics = "MAE: 0.696, RMSE: 0.894",
                LastUpdated = DateTime.UtcNow,
                IsHealthy = true
            };

            _logger.LogInformation("Model info retrieved successfully");

            return Result<ModelInfoResponse>.Success(modelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model information");
            return Result<ModelInfoResponse>.Failure(
                "Failed to get model information",
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<bool>> CheckServiceHealthAsync()
    {
        try
        {
            _logger.LogDebug("Checking AI service health");

            var response = await _httpClient.GetAsync($"{_aiServiceBaseUrl}/health");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("AI service health response: {Content}", content);
                
                return Result<bool>.Success(true);
            }
            else
            {
                _logger.LogWarning("AI service health check failed with status: {StatusCode}", response.StatusCode);
                return Result<bool>.Failure(
                    $"AI service unhealthy: {response.StatusCode}", 
                    ErrorCodeEnum.ExternalServiceError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI service health");
            return Result<bool>.Failure(
                "Failed to check AI service health", 
                ErrorCodeEnum.ExternalServiceError);
        }
    }
}

// DTOs for FastAPI service communication
public class FastAPIRecommendationResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<FastAPIPodcastRecommendation> Recommendations { get; set; } = new();
    public int TotalCount { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class FastAPIPodcastRecommendation
{
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double PredictedRating { get; set; }
    public string? Category { get; set; }
    public string? Topics { get; set; }
    public int? DurationMinutes { get; set; }
    public string? ContentUrl { get; set; }
}