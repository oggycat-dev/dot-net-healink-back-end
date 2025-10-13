using Microsoft.AspNetCore.Mvc;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Domain.Enums;

namespace ContentService.API.Controllers.Internal;

/// <summary>
/// Internal Podcasts Controller - For internal services like AI Recommendation Service
/// </summary>
/// <remarks>
/// This controller is designed for internal microservice communication only.
/// It bypasses user authentication and provides direct access to podcast data
/// for AI/ML recommendation systems.
/// 
/// Security: Should be accessible only within the internal network or via API Gateway.
/// </remarks>
[ApiController]
[Route("api/internal/podcasts")]
[ApiExplorerSettings(GroupName = "Internal")]
public class InternalPodcastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InternalPodcastsController> _logger;

    public InternalPodcastsController(IMediator mediator, ILogger<InternalPodcastsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all published podcasts for AI recommendation system
    /// </summary>
    /// <remarks>
    /// This endpoint returns all PUBLISHED podcasts without authentication.
    /// Designed specifically for the AI Recommendation Service to fetch podcast data.
    /// 
    /// Features:
    /// - No authentication required (internal service)
    /// - Returns only published podcasts
    /// - Supports pagination for large datasets
    /// - Includes all metadata needed for AI recommendations
    /// </remarks>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 100, max: 1000)</param>
    /// <param name="emotionCategories">Optional filter by emotion categories</param>
    /// <param name="topicCategories">Optional filter by topic categories</param>
    /// <returns>Paginated list of published podcasts</returns>
    [HttpGet]
    public async Task<ActionResult<GetPodcastsResponse>> GetAllPublishedPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] EmotionCategory[]? emotionCategories = null,
        [FromQuery] TopicCategory[]? topicCategories = null)
    {
        // Validate page size for internal API (allow larger batches)
        if (pageSize > 1000)
        {
            _logger.LogWarning("Internal API requested page size {PageSize} exceeds maximum 1000", pageSize);
            pageSize = 1000;
        }

        // Only return published podcasts
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.Published,
            EmotionCategories: emotionCategories,
            TopicCategories: topicCategories,
            SearchTerm: null,
            SeriesName: null
        );

        var result = await _mediator.Send(query);
        
        _logger.LogInformation(
            "[INTERNAL] AI Service retrieved {Count} podcasts (Page: {Page}, PageSize: {PageSize}, TotalCount: {TotalCount})",
            result.Podcasts.Count(),
            page,
            pageSize,
            result.TotalCount
        );

        return Ok(result);
    }

    /// <summary>
    /// Get a specific published podcast by ID (Internal access)
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific published podcast.
    /// Does NOT increment view count (internal service access).
    /// </remarks>
    /// <param name="id">Podcast ID</param>
    /// <returns>Podcast details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PodcastDto>> GetPublishedPodcastById(Guid id)
    {
        var query = new GetPodcastByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            _logger.LogWarning("[INTERNAL] Podcast not found: {PodcastId}", id);
            return NotFound(new { message = "Podcast not found" });
        }

        // Only return if published
        if (result.ContentStatus != ContentStatus.Published)
        {
            _logger.LogWarning("[INTERNAL] Attempted to access non-published podcast: {PodcastId}, Status: {Status}", 
                id, result.ContentStatus);
            return NotFound(new { message = "Podcast not available" });
        }

        _logger.LogInformation("[INTERNAL] AI Service accessed podcast: {PodcastId}, Title: {Title}", 
            id, result.Title);

        return Ok(result);
    }

    /// <summary>
    /// Health check for internal API
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { 
            status = "healthy", 
            service = "ContentService.Internal.Podcasts",
            timestamp = DateTime.UtcNow 
        });
    }
}
