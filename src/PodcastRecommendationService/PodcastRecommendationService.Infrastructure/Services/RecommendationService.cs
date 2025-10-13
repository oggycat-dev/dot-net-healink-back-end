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
/// Service implementation that communicates with Python AI service for podcast recommendations
/// </summary>
public class RecommendationService : IRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RecommendationService> _logger;
    private readonly string _aiServiceBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public RecommendationService(
        HttpClient httpClient, 
        ILogger<RecommendationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceBaseUrl = configuration.GetValue<string>("AIService:BaseUrl") ?? "http://podcast-ai-service:8000";
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        _logger.LogInformation("RecommendationService initialized with AI Service URL: {Url}", _aiServiceBaseUrl);
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

            limit = Math.Max(1, Math.Min(limit, 50)); // Ensure limit is between 1-50

            // Build request URL
            var url = $"{_aiServiceBaseUrl}/api/recommendations/{Uri.EscapeDataString(userId)}?limit={limit}&include_listened={includeListened}";
            
            // Make HTTP request to AI service
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AI Service returned error for user {UserId}: {StatusCode} - {Error}", 
                    userId, response.StatusCode, errorContent);
                
                return Result<PodcastRecommendationResponse>.Failure(
                    "Failed to get recommendations from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Parse response
            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("AI Service response for user {UserId}: {Response}", userId, jsonContent);

            var aiResponse = JsonSerializer.Deserialize<AIRecommendationResponse>(jsonContent, _jsonOptions);

            if (aiResponse == null)
            {
                _logger.LogError("Failed to deserialize AI service response for user {UserId}", userId);
                return Result<PodcastRecommendationResponse>.Failure(
                    "Invalid response from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Check for AI service errors
            if (!string.IsNullOrEmpty(aiResponse.Error))
            {
                _logger.LogWarning("AI Service returned error for user {UserId}: {Error}", userId, aiResponse.Error);
                return Result<PodcastRecommendationResponse>.Failure(
                    aiResponse.Error, 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Map to application DTO
            var result = new PodcastRecommendationResponse
            {
                UserId = aiResponse.UserId,
                Recommendations = aiResponse.Recommendations.Select(r => new RecommendationItem
                {
                    PodcastId = r.PodcastId,
                    Title = r.Title,
                    Topic = r.Topic,
                    PredictedRating = r.PredictedRating,
                    ConfidenceScore = r.ConfidenceScore,
                    RecommendationReason = r.RecommendationReason
                }).ToList(),
                TotalFound = aiResponse.TotalFound,
                FilteredListened = aiResponse.FilteredListened,
                GeneratedAt = aiResponse.GeneratedAt
            };

            _logger.LogInformation("Successfully generated {Count} recommendations for user {UserId}", 
                result.Recommendations.Count, userId);

            return Result<PodcastRecommendationResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling AI service for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "AI service is unavailable", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling AI service for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "AI service request timeout", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting recommendations for user {UserId}", userId);
            return Result<PodcastRecommendationResponse>.Failure(
                "An unexpected error occurred", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<BatchRecommendationsResponse>> GetBatchRecommendationsAsync(List<string> userIds, int limit = 10, bool includeListened = false)
    {
        try
        {
            _logger.LogInformation("Getting batch recommendations for {UserCount} users with limit {Limit}", 
                userIds.Count, limit);

            // Validate inputs
            if (userIds == null || !userIds.Any())
            {
                return Result<BatchRecommendationsResponse>.Failure(
                    "User IDs list cannot be empty", 
                    ErrorCodeEnum.ValidationFailed);
            }

            if (userIds.Count > 100)
            {
                return Result<BatchRecommendationsResponse>.Failure(
                    "Maximum 100 users per batch request", 
                    ErrorCodeEnum.ValidationFailed);
            }

            // Remove duplicates and filter out empty/null user IDs
            var validUserIds = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            
            if (!validUserIds.Any())
            {
                return Result<BatchRecommendationsResponse>.Failure(
                    "No valid user IDs provided", 
                    ErrorCodeEnum.ValidationFailed);
            }

            limit = Math.Max(1, Math.Min(limit, 50));

            // Prepare request payload
            var requestData = new
            {
                user_ids = validUserIds,
                limit = limit,
                include_listened = includeListened
            };

            var jsonRequest = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Make HTTP request to AI service
            var response = await _httpClient.PostAsync($"{_aiServiceBaseUrl}/api/recommendations/batch", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Batch AI Service call failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                
                return Result<BatchRecommendationsResponse>.Failure(
                    "Batch recommendation request failed", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Parse response
            var jsonContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<AIBatchRecommendationResponse>(jsonContent, _jsonOptions);

            if (aiResponse?.BatchResults == null)
            {
                _logger.LogError("Failed to deserialize batch AI service response");
                return Result<BatchRecommendationsResponse>.Failure(
                    "Invalid response from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Map to application DTO
            var result = new BatchRecommendationsResponse
            {
                Results = aiResponse.BatchResults.Values.Select(recommendation => new PodcastRecommendationResponse
                {
                    UserId = recommendation.UserId,
                    Recommendations = recommendation.Recommendations?.Select(r => new RecommendationItem
                    {
                        PodcastId = r.PodcastId,
                        Title = r.Title,
                        Topic = r.Topic,
                        PredictedRating = r.PredictedRating,
                        ConfidenceScore = r.ConfidenceScore,
                        RecommendationReason = r.RecommendationReason
                    }).ToList() ?? new List<RecommendationItem>(),
                    TotalFound = recommendation.TotalFound,
                    FilteredListened = recommendation.FilteredListened,
                    GeneratedAt = recommendation.GeneratedAt
                }).ToList(),
                TotalUsers = aiResponse.TotalUsers,
                SuccessfulUsers = aiResponse.BatchResults.Count,
                FailedUsers = new List<string>(),
                GeneratedAt = aiResponse.GeneratedAt
            };

            _logger.LogInformation("Successfully generated batch recommendations for {UserCount} users", 
                result.Results.Count);

            return Result<BatchRecommendationsResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in batch AI service call");
            return Result<BatchRecommendationsResponse>.Failure(
                "AI service is unavailable", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout in batch AI service call");
            return Result<BatchRecommendationsResponse>.Failure(
                "AI service request timeout", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in batch recommendation call");
            return Result<BatchRecommendationsResponse>.Failure(
                "An unexpected error occurred", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<bool>> TrackInteractionAsync(string userId, RecommendationInteractionRequest request)
    {
        try
        {
            _logger.LogInformation("Tracking interaction for user {UserId} with podcast {PodcastId} - {InteractionType}", 
                userId, request.PodcastId, request.InteractionType);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID cannot be empty", ErrorCodeEnum.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.PodcastId))
            {
                return Result<bool>.Failure("Podcast ID cannot be empty", ErrorCodeEnum.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(request.InteractionType))
            {
                return Result<bool>.Failure("Interaction type cannot be empty", ErrorCodeEnum.ValidationFailed);
            }

            // TODO: Implement interaction tracking (could be stored in database for analytics)
            // For now, just log the interaction
            _logger.LogInformation("Interaction tracked: User {UserId} performed {InteractionType} on podcast {PodcastId}", 
                userId, request.InteractionType, request.PodcastId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking interaction for user {UserId}", userId);
            return Result<bool>.Failure("Failed to track interaction", ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<UserListenedPodcastsResponse>> GetUserListenedPodcastsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting listened podcasts for user {UserId}", userId);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<UserListenedPodcastsResponse>.Failure(
                    "User ID cannot be empty", 
                    ErrorCodeEnum.ValidationFailed);
            }

            // Make HTTP request to AI service
            var url = $"{_aiServiceBaseUrl}/api/users/{Uri.EscapeDataString(userId)}/listened";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AI Service returned error for user listened podcasts {UserId}: {StatusCode} - {Error}", 
                    userId, response.StatusCode, errorContent);
                
                return Result<UserListenedPodcastsResponse>.Failure(
                    "Failed to get listened podcasts from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Parse response
            var jsonContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<AIUserListenedResponse>(jsonContent, _jsonOptions);

            if (aiResponse == null)
            {
                return Result<UserListenedPodcastsResponse>.Failure(
                    "Invalid response from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Map to application DTO
            var result = new UserListenedPodcastsResponse
            {
                UserId = aiResponse.UserId,
                ListenedPodcasts = aiResponse.ListenedPodcasts.Select(p => p.PodcastId).ToList(),
                TotalListened = aiResponse.TotalListened,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Found {Count} listened podcasts for user {UserId}", 
                result.TotalListened, userId);

            return Result<UserListenedPodcastsResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting user listened podcasts for {UserId}", userId);
            return Result<UserListenedPodcastsResponse>.Failure(
                "AI service is unavailable", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user listened podcasts for {UserId}", userId);
            return Result<UserListenedPodcastsResponse>.Failure(
                "An unexpected error occurred", 
                ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<ModelInfoResponse>> GetModelInfoAsync()
    {
        try
        {
            _logger.LogInformation("Getting AI model information");

            // Make HTTP request to AI service
            var response = await _httpClient.GetAsync($"{_aiServiceBaseUrl}/api/model/info");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AI Service returned error for model info: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                
                return Result<ModelInfoResponse>.Failure(
                    "Failed to get model info from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Parse response
            var jsonContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<AIModelInfoResponse>(jsonContent, _jsonOptions);

            if (aiResponse == null)
            {
                return Result<ModelInfoResponse>.Failure(
                    "Invalid response from AI service", 
                    ErrorCodeEnum.ExternalServiceError);
            }

            // Map to application DTO
            var result = new ModelInfoResponse
            {
                ModelType = "collaborative_filtering",
                ModelVersion = "1.0.0",
                Framework = "pandas/numpy", 
                TrainingDataStats = $"Users: {aiResponse.TotalUsers}, Podcasts: {aiResponse.TotalPodcasts}, Ratings: {aiResponse.TotalRatings}",
                PerformanceMetrics = aiResponse.ModelSummary != null ? 
                    $"Input: {aiResponse.ModelSummary.InputShape}, Output: {aiResponse.ModelSummary.OutputShape}, Params: {aiResponse.ModelSummary.TotalParams}" : 
                    "No metrics available",
                LastUpdated = DateTime.UtcNow,
                IsHealthy = aiResponse.Status == "healthy"
            };

            return Result<ModelInfoResponse>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting model info");
            return Result<ModelInfoResponse>.Failure(
                "AI service is unavailable", 
                ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info");
            return Result<ModelInfoResponse>.Failure(
                "An unexpected error occurred", 
                ErrorCodeEnum.InternalError);
        }
    }
}

// Internal DTOs for AI service communication
internal class AIRecommendationResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<AIRecommendationItem> Recommendations { get; set; } = new();
    public int TotalFound { get; set; }
    public bool FilteredListened { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Error { get; set; }
}

internal class AIRecommendationItem
{
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public decimal PredictedRating { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
}

internal class AIBatchRecommendationResponse
{
    public Dictionary<string, AIRecommendationResponse> BatchResults { get; set; } = new();
    public int TotalUsers { get; set; }
    public DateTime GeneratedAt { get; set; }
}

internal class AIUserListenedResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<AIListenedPodcastItem> ListenedPodcasts { get; set; } = new();
    public int TotalListened { get; set; }
}

internal class AIListenedPodcastItem
{
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}

internal class AIModelInfoResponse
{
    public string Status { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int TotalPodcasts { get; set; }
    public int TotalRatings { get; set; }
    public AIModelSummary? ModelSummary { get; set; }
}

internal class AIModelSummary
{
    public string? InputShape { get; set; }
    public string? OutputShape { get; set; }
    public int? TotalParams { get; set; }
}