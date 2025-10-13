using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PodcastRecommendationService.Application.Services;
using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Enums;

namespace PodcastRecommendationService.API.Controllers;

/// <summary>
/// Internal data controller for AI service data fetching
/// </summary>
[ApiController]
[Route("api/internal/[controller]")]
public class DataController : ControllerBase
{
    private readonly IDataFetchService _dataFetchService;
    private readonly ILogger<DataController> _logger;

    public DataController(
        IDataFetchService dataFetchService,
        ILogger<DataController> logger)
    {
        _dataFetchService = dataFetchService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users for AI model training (Internal use only)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Internal request: Getting all users for AI training");

            var result = await _dataFetchService.GetAllUsersAsync();
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch users: {Error}", result.Message);
                return BadRequest(result);
            }

            return Ok(Result<List<UserDataDto>>.Success(result.Data!, "Users retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users for AI training");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get all podcasts for AI model training (Internal use only)
    /// </summary>
    [HttpGet("podcasts")]
    public async Task<IActionResult> GetAllPodcasts()
    {
        try
        {
            _logger.LogInformation("Internal request: Getting all podcasts for AI training");

            var result = await _dataFetchService.GetAllPodcastsAsync();
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch podcasts: {Error}", result.Message);
                return BadRequest(result);
            }

            return Ok(Result<List<PodcastDataDto>>.Success(result.Data!, "Podcasts retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all podcasts for AI training");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get user ratings/interactions for AI model training (Internal use only)
    /// </summary>
    [HttpGet("ratings")]
    public async Task<IActionResult> GetUserRatings()
    {
        try
        {
            _logger.LogInformation("Internal request: Getting user ratings for AI training");

            var result = await _dataFetchService.GetUserRatingsAsync();
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch user ratings: {Error}", result.Message);
                return BadRequest(result);
            }

            return Ok(Result<List<UserRatingDto>>.Success(result.Data!, "User ratings retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ratings for AI training");
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get specific user data (Internal use only)
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(Result<object>.Failure("User ID is required", ErrorCodeEnum.ValidationFailed));
            }

            _logger.LogInformation("Internal request: Getting user {UserId} data", userId);

            var result = await _dataFetchService.GetUserByIdAsync(userId);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorCode == ErrorCodeEnum.NotFound.ToString())
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(Result<UserDataDto>.Success(result.Data!, "User retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} for AI service", userId);
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Get specific podcast data (Internal use only)
    /// </summary>
    [HttpGet("podcasts/{podcastId}")]
    public async Task<IActionResult> GetPodcastById(string podcastId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(podcastId))
            {
                return BadRequest(Result<object>.Failure("Podcast ID is required", ErrorCodeEnum.ValidationFailed));
            }

            _logger.LogInformation("Internal request: Getting podcast {PodcastId} data", podcastId);

            var result = await _dataFetchService.GetPodcastByIdAsync(podcastId);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorCode == ErrorCodeEnum.NotFound.ToString())
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(Result<PodcastDataDto>.Success(result.Data!, "Podcast retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting podcast {PodcastId} for AI service", podcastId);
            return StatusCode(500, Result<object>.Failure("An unexpected error occurred", ErrorCodeEnum.InternalError));
        }
    }

    /// <summary>
    /// Health check for data services
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> DataHealthCheck()
    {
        try
        {
            // Test connectivity to dependent services
            var usersResult = await _dataFetchService.GetAllUsersAsync();
            var podcastsResult = await _dataFetchService.GetAllPodcastsAsync();

            var health = new
            {
                Status = "healthy",
                Services = new
                {
                    UserService = usersResult.IsSuccess ? "healthy" : "unhealthy",
                    ContentService = podcastsResult.IsSuccess ? "healthy" : "unhealthy"
                },
                Timestamp = DateTime.UtcNow
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data health check");
            return StatusCode(500, new { Status = "unhealthy", Error = ex.Message });
        }
    }
}