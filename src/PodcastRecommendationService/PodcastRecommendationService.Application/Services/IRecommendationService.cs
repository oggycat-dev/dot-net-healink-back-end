using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Models;

namespace PodcastRecommendationService.Application.Services;

/// <summary>
/// Service interface for podcast recommendations
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Get personalized podcast recommendations for a user
    /// </summary>
    Task<Result<PodcastRecommendationResponse>> GetRecommendationsAsync(string userId, int limit = 10, bool includeListened = false);
    
    /// <summary>
    /// Get recommendations for multiple users in batch
    /// </summary>
    Task<Result<BatchRecommendationsResponse>> GetBatchRecommendationsAsync(List<string> userIds, int limit = 10, bool includeListened = false);
    
    /// <summary>
    /// Track user interaction with a recommendation
    /// </summary>
    Task<Result<bool>> TrackInteractionAsync(string userId, RecommendationInteractionRequest request);
    
    /// <summary>
    /// Get list of podcasts already listened by user
    /// </summary>
    Task<Result<UserListenedPodcastsResponse>> GetUserListenedPodcastsAsync(string userId);
    
    /// <summary>
    /// Get AI model information and statistics
    /// </summary>
    Task<Result<ModelInfoResponse>> GetModelInfoAsync();
}