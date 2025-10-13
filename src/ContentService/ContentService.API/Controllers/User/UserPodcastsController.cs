using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Domain.Enums;

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
    private readonly ILogger<UserPodcastsController> _logger;

    public UserPodcastsController(IMediator mediator, ILogger<UserPodcastsController> logger)
    {
        _mediator = mediator;
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

    // TODO: Implement interaction endpoints
    // POST {id}/view - Track view count
    // POST {id}/like - Like podcast
    // POST {id}/favorite - Add to favorites
    // GET /favorites - Get user's favorite podcasts (requires authentication)
}
