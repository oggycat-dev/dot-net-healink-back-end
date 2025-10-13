using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Models;

namespace PodcastRecommendationService.Application.Services;

/// <summary>
/// Service interface for fetching real data from other microservices
/// </summary>
public interface IDataFetchService
{
    /// <summary>
    /// Get all users from UserService
    /// </summary>
    Task<Result<List<UserDataDto>>> GetAllUsersAsync();
    
    /// <summary>
    /// Get all podcasts from ContentService
    /// </summary>
    Task<Result<List<PodcastDataDto>>> GetAllPodcastsAsync();
    
    /// <summary>
    /// Get user interactions/ratings (could be from analytics or user behavior)
    /// </summary>
    Task<Result<List<UserRatingDto>>> GetUserRatingsAsync();
    
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<Result<UserDataDto>> GetUserByIdAsync(string userId);
    
    /// <summary>
    /// Get podcast by ID  
    /// </summary>
    Task<Result<PodcastDataDto>> GetPodcastByIdAsync(string podcastId);
}