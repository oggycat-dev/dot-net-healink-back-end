using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PodcastRecommendationService.Application.Services;
using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Services;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Enums;
using System.ComponentModel.DataAnnotations;

namespace PodcastRecommendationService.API.Controllers;

/// <summary>
/// Controller for managing podcast recommendations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ICurrentUserService currentUserService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get personalized podcast recommendations for the current user
    /// </summary>
    /// <param name="limit">Maximum number of recommendations to return (1-50)</param>
    /// <param name="includeListened">Whether to include already listened podcasts</param>
    /// <returns>List of personalized podcast recommendations</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(Result<PodcastRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyRecommendations(
        [FromQuery, Range(1, 50)] int limit = 10,
        [FromQuery] bool includeListened = false)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthenticated user attempted to get recommendations");
                return Unauthorized(Result<object>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized));
            }

            _logger.LogInformation("Getting recommendations for user {UserId} with limit {Limit}, includeListened: {IncludeListened}", 
                userId, limit, includeListened);

            var result = await _recommendationService.GetRecommendationsAsync(userId, limit, includeListened);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get recommendations for user {UserId}: {Error}", userId, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Successfully retrieved {Count} recommendations for user {UserId}", 
                result.Data?.TotalFound ?? 0, userId);

            return Ok(Result<PodcastRecommendationResponse>.Success(result.Data!, "Recommendations retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting recommendations for current user");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get recommendations for a specific user (Admin/System only)
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="limit">Maximum number of recommendations to return (1-50)</param>
    /// <param name="includeListened">Whether to include already listened podcasts</param>
    /// <returns>List of podcast recommendations for the specified user</returns>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<PodcastRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserRecommendations(
        [FromRoute] string userId, 
        [FromQuery, Range(1, 50)] int limit = 10,
        [FromQuery] bool includeListened = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(Result<object>.Failure("User ID is required", ErrorCodeEnum.ValidationFailed));
            }

            _logger.LogInformation("Admin getting recommendations for user {UserId} with limit {Limit}", userId, limit);

            var result = await _recommendationService.GetRecommendationsAsync(userId, limit, includeListened);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get recommendations for user {UserId}: {Error}", userId, result.Message);
                return BadRequest(result);
            }

            return Ok(Result<PodcastRecommendationResponse>.Success(result.Data!, "User recommendations retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting recommendations for user {UserId}", userId);
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get batch recommendations for multiple users (Admin/System only)
    /// </summary>
    /// <param name="request">Batch recommendation request containing user IDs and parameters</param>
    /// <returns>Batch recommendations for multiple users</returns>
    [HttpPost("batch")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<BatchRecommendationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBatchRecommendations([FromBody] BatchRecommendationsRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(Result<object>.Failure("Request body is required", ErrorCodeEnum.ValidationFailed));
            }

            if (request.UserIds == null || !request.UserIds.Any())
            {
                return BadRequest(Result<object>.Failure("User IDs are required", ErrorCodeEnum.ValidationFailed));
            }

            if (request.UserIds.Count > 100)
            {
                return BadRequest(Result<object>.Failure("Maximum 100 users per batch request", ErrorCodeEnum.ValidationFailed));
            }

            _logger.LogInformation("Processing batch recommendations for {UserCount} users", request.UserIds.Count);

            var result = await _recommendationService.GetBatchRecommendationsAsync(
                request.UserIds, 
                Math.Max(1, Math.Min(request.Limit, 50)), 
                request.IncludeListened);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get batch recommendations: {Error}", result.Message);
                return BadRequest(result);
            }

            return Ok(Result<BatchRecommendationsResponse>.Success(result.Data!, "Batch recommendations retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting batch recommendations");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Track user interaction with a recommendation
    /// </summary>
    /// <param name="request">Interaction tracking request</param>
    /// <returns>Success status of interaction tracking</returns>
    [HttpPost("interaction")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrackInteraction([FromBody] RecommendationInteractionRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Result<object>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized));
            }

            if (request == null)
            {
                return BadRequest(Result<object>.Failure("Request body is required", ErrorCodeEnum.ValidationFailed));
            }

            if (string.IsNullOrWhiteSpace(request.PodcastId))
            {
                return BadRequest(Result<object>.Failure("Podcast ID is required", ErrorCodeEnum.ValidationFailed));
            }

            if (string.IsNullOrWhiteSpace(request.InteractionType))
            {
                return BadRequest(Result<object>.Failure("Interaction type is required", ErrorCodeEnum.ValidationFailed));
            }

            _logger.LogInformation("Tracking interaction for user {UserId}: {InteractionType} on podcast {PodcastId}", 
                userId, request.InteractionType, request.PodcastId);

            var result = await _recommendationService.TrackInteractionAsync(userId, request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to track interaction for user {UserId}: {Error}", userId, result.Message);
                return BadRequest(result);
            }

            return Ok(Result<bool>.Success(result.Data, "Interaction tracked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error tracking interaction");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get list of podcasts already listened by the current user
    /// </summary>
    /// <returns>List of podcasts listened by the user</returns>
    [HttpGet("me/listened")]
    [ProducesResponseType(typeof(Result<UserListenedPodcastsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyListenedPodcasts()
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(Result<object>.Failure("User not authenticated", ErrorCodeEnum.Unauthorized));
            }

            _logger.LogInformation("Getting listened podcasts for user {UserId}", userId);

            var result = await _recommendationService.GetUserListenedPodcastsAsync(userId);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get listened podcasts for user {UserId}: {Error}", userId, result.Message);
                return BadRequest(result);
            }

            return Ok(Result<UserListenedPodcastsResponse>.Success(result.Data!, "Listened podcasts retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting listened podcasts for current user");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get AI model information and statistics (Admin only)
    /// </summary>
    /// <returns>AI model information including training statistics</returns>
    [HttpGet("model/info")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<ModelInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModelInfo()
    {
        try
        {
            _logger.LogInformation("Getting AI model information");

            var result = await _recommendationService.GetModelInfoAsync();
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get model info: {Error}", result.Message);
                return BadRequest(result);
            }

            return Ok(Result<ModelInfoResponse>.Success(result.Data!, "Model information retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting model info");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }
}