using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Application.Features.Podcasts.Commands;
using SharedLibrary.Commons.Services;
using ContentService.API.DTOs;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PodcastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PodcastsController> _logger;

    public PodcastsController(IMediator mediator, ICurrentUserService currentUserService, ILogger<PodcastsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Debug endpoint to check user claims and roles
    /// </summary>
    [HttpGet("debug/me")]
    [Authorize]
    public IActionResult GetMyInfo()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var userId = _currentUserService.UserId;
        var roles = _currentUserService.Roles;
        var isAuthenticated = _currentUserService.IsAuthenticated;
        
        return Ok(new 
        { 
            UserId = userId,
            Roles = roles,
            IsAuthenticated = isAuthenticated,
            Claims = claims,
            HasContentCreatorRole = roles?.Contains("ContentCreator") ?? false,
            HasContentCreatorClaim = User.Claims.Any(c => c.Type == "role" && c.Value == "ContentCreator")
        });
    }

    /// <summary>
    /// Get podcasts with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GetPodcastsResponse>> GetPodcasts([FromQuery] GetPodcastsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific podcast by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PodcastDto>> GetPodcast(Guid id)
    {
        var query = new GetPodcastByIdQuery(id);
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Create a new podcast with audio file upload (Content Creator/Expert only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ContentCreator")]
    [RequestSizeLimit(500_000_000)] // 500MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<ActionResult<CreatePodcastResponse>> CreatePodcast([FromForm] CreatePodcastRequest request)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }

        // Log received request data for debugging
        _logger.LogWarning("=== [DEBUG] CreatePodcast Request Received ===");
        _logger.LogWarning("  Title: {Title}", request.Title);
        _logger.LogWarning("  Description: {Description}", request.Description);
        _logger.LogWarning("  HostName: '{HostName}'", request.HostName ?? "NULL");
        _logger.LogWarning("  GuestName: '{GuestName}'", request.GuestName ?? "NULL");
        _logger.LogWarning("  EpisodeNumber: {EpisodeNumber}", request.EpisodeNumber);
        _logger.LogWarning("  SeriesName: '{SeriesName}'", request.SeriesName ?? "NULL");
        _logger.LogWarning("  TranscriptUrl: '{TranscriptUrl}'", request.TranscriptUrl ?? "NULL");
        _logger.LogWarning("  Tags count: {TagsCount}", request.Tags?.Count ?? 0);
        _logger.LogWarning("  EmotionCategories count: {EmotionCount}", request.EmotionCategories?.Count ?? 0);
        _logger.LogWarning("  TopicCategories count: {TopicCount}", request.TopicCategories?.Count ?? 0);
        _logger.LogWarning("  ThumbnailFile: {HasThumbnail}", request.ThumbnailFile != null ? "Provided" : "NULL");
        _logger.LogWarning("=== [DEBUG] End ===");

        // Validate audio file
        if (request.AudioFile == null || request.AudioFile.Length == 0)
        {
            return BadRequest("Audio file is required");
        }

        var allowedContentTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a" };
        if (!allowedContentTypes.Contains(request.AudioFile.ContentType.ToLower()))
        {
            return BadRequest("Invalid audio file format. Supported formats: MP3, WAV, OGG, M4A");
        }

        var command = new CreatePodcastCommand(
            request.Title,
            request.Description,
            request.AudioFile,
            TimeSpan.FromSeconds(request.Duration),
            request.TranscriptUrl,
            request.HostName,
            request.GuestName,
            request.EpisodeNumber,
            request.SeriesName,
            request.Tags?.ToArray(),
            request.EmotionCategories?.ToArray(),
            request.TopicCategories?.ToArray(),
            request.ThumbnailFile
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPodcast), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing podcast (owner only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "ContentCreator")]
    public async Task<ActionResult<UpdatePodcastResponse>> UpdatePodcast(Guid id, [FromForm] UpdatePodcastRequest request)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }

        // Validate audio file if provided
        if (request.AudioFile != null && request.AudioFile.Length > 0)
        {
            var allowedContentTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a" };
            if (!allowedContentTypes.Contains(request.AudioFile.ContentType.ToLower()))
            {
                return BadRequest("Invalid audio file format. Supported formats: MP3, WAV, OGG, M4A");
            }
        }

        var command = new UpdatePodcastCommand(
            id,
            request.Title,
            request.Description,
            request.Duration.HasValue ? TimeSpan.FromSeconds(request.Duration.Value) : null,
            null, // TranscriptUrl
            null, // HostName
            null, // GuestName
            null, // EpisodeNumber
            null, // SeriesName
            request.Tags?.ToArray(),
            new Domain.Enums.EmotionCategory[0], // EmotionCategories
            new Domain.Enums.TopicCategory[0],   // TopicCategories
            null, // AudioFile
            null, // ThumbnailFile
            userId
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a podcast (owner only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "ContentCreator")]
    public async Task<ActionResult> DeletePodcast(Guid id)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }
        
        var command = new DeletePodcastCommand(id, userId);
        var result = await _mediator.Send(command);
        
        if (!result)
            return NotFound("Podcast not found or you don't have permission to delete it");

        return NoContent();
    }

    /// <summary>
    /// Approve a podcast (Moderator only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "CommunityModerator")]
    public async Task<ActionResult<ApprovePodcastResponse>> ApprovePodcast(Guid id, [FromBody] ApprovePodcastRequest request)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }

        var command = new ApprovePodcastCommand(id, userId, request.ApprovalNotes);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Reject a podcast (Moderator only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Policy = "CommunityModerator")]
    public async Task<ActionResult> RejectPodcast(Guid id, [FromBody] RejectPodcastRequest request)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }

        var command = new RejectPodcastCommand(id, userId, request.RejectionReason);
        var result = await _mediator.Send(command);
        
        if (!result)
            return NotFound("Podcast not found");

        return NoContent();
    }

    /// <summary>
    /// Get related flashcards for a podcast
    /// </summary>
    [HttpGet("{id}/related-flashcards")]
    public async Task<ActionResult> GetRelatedFlashcards(Guid id)
    {
        // Implementation will be added later when flashcard service is ready
        return Ok(new { message = "Related flashcards feature coming soon", podcastId = id });
    }

    /// <summary>
    /// Get podcast analytics (creator/moderator only)
    /// </summary>
    [HttpGet("{id}/analytics")]
    [Authorize(Policy = "ContentCreator")]
    public async Task<ActionResult> GetPodcastAnalytics(Guid id)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized();
        }

        // Implementation will be added later with analytics service
        return Ok(new { message = "Analytics feature coming soon", podcastId = id });
    }

    /// <summary>
    /// Increment view count for a podcast (public)
    /// </summary>
    [HttpPost("{id}/views")]
    public async Task<ActionResult> IncrementView(Guid id)
    {
        var command = new IncrementPodcastViewCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Toggle like for a podcast (requires authentication)
    /// POST will like if not liked, unlike if already liked
    /// </summary>
    [HttpPost("{id}/likes")]
    [Authorize]
    public async Task<ActionResult<int>> ToggleLike(Guid id)
    {
        if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
            return Unauthorized();

        var command = new TogglePodcastLikeCommand(id, userId);
        var newLikeCount = await _mediator.Send(command);
        return Ok(new { likes = newLikeCount });
    }
}