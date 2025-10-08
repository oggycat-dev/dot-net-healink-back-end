using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.API.DTOs;
using SharedLibrary.Commons.Services;
using ContentService.Domain.Enums;

namespace ContentService.API.Controllers.Creator;

/// <summary>
/// Creator Podcasts Controller - For content creators to manage their podcasts
/// </summary>
[ApiController]
[Route("api/creator/podcasts")]
[Authorize(Policy = "ContentCreator")]
[ApiExplorerSettings(GroupName = "Creator")]
public class CreatorPodcastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatorPodcastsController> _logger;

    public CreatorPodcastsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CreatorPodcastsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get my podcasts as a content creator
    /// </summary>
    /// <remarks>
    /// Returns all podcasts created by the authenticated content creator, regardless of status.
    /// Includes drafts, pending review, approved, rejected, etc.
    /// </remarks>
    [HttpGet("my-podcasts")]
    public async Task<ActionResult<GetPodcastsResponse>> GetMyPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] string? searchTerm = null)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // TODO: Add CreatedBy filter to GetPodcastsQuery
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: status,
            SearchTerm: searchTerm
        );

        var result = await _mediator.Send(query);
        
        // Filter by creator ID (temporary until query supports it)
        var myPodcasts = result.Podcasts.Where(p => p.CreatedBy == creatorId);

        _logger.LogInformation(
            "Creator {CreatorId} retrieved their podcasts: {Count} results",
            creatorId,
            myPodcasts.Count()
        );

        return Ok(new GetPodcastsResponse(
            Podcasts: myPodcasts,
            TotalCount: myPodcasts.Count(),
            Page: page,
            PageSize: pageSize
        ));
    }

    /// <summary>
    /// Get a specific podcast by ID (creator can see their own regardless of status)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PodcastDto>> GetMyPodcast(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var query = new GetPodcastByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            _logger.LogWarning("Podcast not found: {PodcastId}", id);
            return NotFound(new { message = "Podcast not found" });
        }

        // Check ownership
        if (result.CreatedBy != creatorId)
        {
            _logger.LogWarning(
                "Creator {CreatorId} attempted to access podcast {PodcastId} owned by {OwnerId}",
                creatorId,
                id,
                result.CreatedBy
            );
            return Forbid();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new podcast with audio file upload
    /// </summary>
    /// <remarks>
    /// Upload a new podcast episode with audio file. Supported formats: MP3, WAV, OGG, M4A, MP4.
    /// Maximum file size: 500MB.
    /// </remarks>
    [HttpPost]
    [RequestSizeLimit(500_000_000)] // 500MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<ActionResult<CreatePodcastResponse>> CreatePodcast([FromForm] CreatePodcastRequest request)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // Validate audio file
        if (request.AudioFile == null || request.AudioFile.Length == 0)
        {
            return BadRequest(new { message = "Audio file is required" });
        }

        var allowedContentTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a", "audio/x-m4a", "audio/mp4" };
        if (!allowedContentTypes.Contains(request.AudioFile.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid audio file format. Supported formats: MP3, WAV, OGG, M4A, MP4" });
        }

        // Log received request for debugging
        _logger.LogInformation(
            "Creating podcast - Title: {Title}, Host: {Host}, Guest: {Guest}, Episode: {Episode}, Series: {Series}, Transcript: {Transcript}, Tags: {Tags}, ThumbnailFile: {HasThumbnail}",
            request.Title,
            request.HostName ?? "NULL",
            request.GuestName ?? "NULL",
            request.EpisodeNumber,
            request.SeriesName ?? "NULL",
            request.TranscriptUrl ?? "NULL",
            request.Tags?.Count ?? 0,
            request.ThumbnailFile != null ? "Yes" : "No"
        );

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

        _logger.LogInformation(
            "Creator {CreatorId} created new podcast: {PodcastId}, Title: {Title}",
            creatorId,
            result.Id,
            result.Title
        );

        return CreatedAtAction(nameof(GetMyPodcast), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing podcast
    /// </summary>
    /// <remarks>
    /// Update podcast details. Can optionally update the audio file and thumbnail.
    /// Only the podcast owner can update it.
    /// </remarks>
    [HttpPut("{id}")]
    public async Task<ActionResult<UpdatePodcastResponse>> UpdatePodcast(Guid id, [FromForm] UpdatePodcastRequest request)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // Validate audio file if provided
        if (request.AudioFile != null && request.AudioFile.Length > 0)
        {
            var allowedAudioTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a", "audio/x-m4a", "audio/mp4" };
            if (!allowedAudioTypes.Contains(request.AudioFile.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Invalid audio file format. Supported formats: MP3, WAV, OGG, M4A, MP4" });
            }
        }

        // Log update request
        _logger.LogInformation(
            "Updating podcast {PodcastId} - Title: {Title}, Host: {Host}, Guest: {Guest}, Episode: {Episode}, Series: {Series}",
            id,
            request.Title ?? "unchanged",
            request.HostName ?? "unchanged",
            request.GuestName ?? "unchanged",
            request.EpisodeNumber?.ToString() ?? "unchanged",
            request.SeriesName ?? "unchanged"
        );

        var command = new UpdatePodcastCommand(
            id,
            request.Title,
            request.Description,
            request.Duration.HasValue ? TimeSpan.FromSeconds(request.Duration.Value) : null,
            request.TranscriptUrl,
            request.HostName,
            request.GuestName,
            request.EpisodeNumber,
            request.SeriesName,
            request.Tags?.ToArray(),
            request.EmotionCategories?.ToArray(),
            request.TopicCategories?.ToArray(),
            request.AudioFile,
            request.ThumbnailFile,
            creatorId
        );

        var result = await _mediator.Send(command);

        _logger.LogInformation(
            "Creator {CreatorId} updated podcast: {PodcastId}",
            creatorId,
            id
        );

        return Ok(result);
    }

    /// <summary>
    /// Delete a podcast
    /// </summary>
    /// <remarks>
    /// Permanently delete a podcast. Only the podcast owner can delete it.
    /// This action cannot be undone.
    /// </remarks>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePodcast(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var command = new DeletePodcastCommand(id, creatorId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            _logger.LogWarning(
                "Creator {CreatorId} failed to delete podcast: {PodcastId} - Not found or no permission",
                creatorId,
                id
            );
            return NotFound(new { message = "Podcast not found or you don't have permission to delete it" });
        }

        _logger.LogInformation(
            "Creator {CreatorId} deleted podcast: {PodcastId}",
            creatorId,
            id
        );

        return NoContent();
    }

    /// <summary>
    /// Get my podcast statistics
    /// </summary>
    [HttpGet("{id}/stats")]
    public async Task<ActionResult> GetPodcastStats(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // Check ownership
        var query = new GetPodcastByIdQuery(id);
        var podcast = await _mediator.Send(query);

        if (podcast == null)
        {
            return NotFound(new { message = "Podcast not found" });
        }

        if (podcast.CreatedBy != creatorId)
        {
            return Forbid();
        }

        // TODO: Implement detailed analytics
        return Ok(new
        {
            podcastId = id,
            viewCount = podcast.ViewCount,
            likeCount = podcast.LikeCount,
            status = podcast.ContentStatus.ToString(),
            message = "Detailed analytics coming soon"
        });
    }

    /// <summary>
    /// Get dashboard summary for creator
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // TODO: Implement dashboard with statistics
        _logger.LogInformation("Creator {CreatorId} accessed dashboard", creatorId);

        return Ok(new
        {
            message = "Creator dashboard coming soon",
            creatorId = creatorId,
            totalPodcasts = 0,
            publishedPodcasts = 0,
            pendingPodcasts = 0,
            totalViews = 0,
            totalLikes = 0
        });
    }
}
