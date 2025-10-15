using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Services;

namespace ContentService.API.Controllers.User;

/// <summary>
/// User Podcasts Controller - For regular users to browse and listen to podcasts
/// </summary>
[ApiController]
[Route("api/user/podcasts")]
[ApiExplorerSettings(GroupName = "User")]
public class UserPodcastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserPodcastsController> _logger;

    public UserPodcastsController(
        IMediator mediator, 
        ICurrentUserService currentUserService,
        ILogger<UserPodcastsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get published podcasts for listening (Public access)
    /// </summary>
    /// <remarks>
    /// This endpoint returns only PUBLISHED podcasts that are approved and ready for users to listen.
    /// Supports filtering by emotion categories, topic categories, and search terms.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetPublishedPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] EmotionCategory[]? emotionCategories = null,
        [FromQuery] TopicCategory[]? topicCategories = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? seriesName = null)
    {
        // Only show published podcasts to regular users
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            EmotionCategories: emotionCategories,
            TopicCategories: topicCategories,
            SearchTerm: searchTerm,
            SeriesName: seriesName
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "User retrieved {Count} published podcasts (Page: {Page}, PageSize: {PageSize})",
            result.Podcasts.Count(),
            page,
            pageSize
        );

        return Ok(result);
    }

    /// <summary>
    /// Get a specific published podcast by ID (Public access)
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific published podcast.
    /// This will increment the view count automatically.
    /// </remarks>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PodcastDto>> GetPublishedPodcast(Guid id)
    {
        var query = new GetPodcastByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            _logger.LogWarning("Podcast not found: {PodcastId}", id);
            return NotFound(new { message = "Podcast not found" });
        }

        // Only return if published
        if (result.ContentStatus != ContentStatus.Published)
        {
            _logger.LogWarning("Attempted to access non-published podcast: {PodcastId}, Status: {Status}", id, result.ContentStatus);
            return NotFound(new { message = "Podcast not available" });
        }

        _logger.LogInformation("User viewed podcast: {PodcastId}, Title: {Title}", id, result.Title);

        return Ok(result);
    }

    /// <summary>
    /// Get podcasts by emotion category
    /// </summary>
    [HttpGet("by-emotion/{emotion}")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetPodcastsByEmotion(
        EmotionCategory emotion,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            EmotionCategories: new[] { emotion }
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "User retrieved podcasts by emotion {Emotion}: {Count} results",
            emotion,
            result.Podcasts.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get podcasts by topic category
    /// </summary>
    [HttpGet("by-topic/{topic}")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetPodcastsByTopic(
        TopicCategory topic,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            TopicCategories: new[] { topic }
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "User retrieved podcasts by topic {Topic}: {Count} results",
            topic,
            result.Podcasts.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get podcasts by series name
    /// </summary>
    [HttpGet("series/{seriesName}")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetPodcastsBySeries(
        string seriesName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            SeriesName: seriesName
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "User retrieved podcasts in series '{Series}': {Count} results",
            seriesName,
            result.Podcasts.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Search podcasts by keyword
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> SearchPodcasts(
        [FromQuery] string keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { message = "Search keyword is required" });
        }

        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            SearchTerm: keyword
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "User searched podcasts with keyword '{Keyword}': {Count} results",
            keyword,
            result.Podcasts.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get trending/popular podcasts (most viewed)
    /// </summary>
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetTrendingPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // For now, get published podcasts
        // TODO: Implement sorting by view count in repository
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation("User retrieved trending podcasts: {Count} results", result.Podcasts.Count());

        return Ok(result);
    }

    /// <summary>
    /// Get latest published podcasts
    /// </summary>
    [HttpGet("latest")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPodcastsResponse>> GetLatestPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation("User retrieved latest podcasts: {Count} results", result.Podcasts.Count());

        return Ok(result);
    }

    /// <summary>
    /// Increment view count for a podcast (Public access)
    /// </summary>
    /// <remarks>
    /// This endpoint should be called when a user starts playing or viewing a podcast.
    /// It increments the view count and records an anonymous view interaction.
    /// </remarks>
    [HttpPost("{id}/views")]
    [AllowAnonymous]
    public async Task<ActionResult> IncrementView(Guid id)
    {
        var command = new IncrementPodcastViewCommand(id);
        await _mediator.Send(command);
        
        _logger.LogInformation("View recorded for podcast: {PodcastId}", id);
        
        return NoContent();
    }

    /// <summary>
    /// Toggle like/unlike for a podcast (Requires authentication)
    /// </summary>
    /// <remarks>
    /// If the user has not liked the podcast, this will add a like.
    /// If the user has already liked it, this will remove the like (unlike).
    /// Returns the current total like count after the operation.
    /// </remarks>
    [HttpPost("{id}/likes")]
    [Authorize]
    public async Task<ActionResult<object>> ToggleLike(Guid id)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            _logger.LogWarning("Unauthorized like attempt for podcast: {PodcastId}", id);
            return Unauthorized(new { message = "User must be authenticated to like podcasts" });
        }

        var command = new TogglePodcastLikeCommand(id, userId);
        var newLikeCount = await _mediator.Send(command);
        
        _logger.LogInformation("Like toggled for podcast: {PodcastId} by user: {UserId}, New count: {LikeCount}", 
            id, userId, newLikeCount);
        
        return Ok(new { likes = newLikeCount });
    }
}
