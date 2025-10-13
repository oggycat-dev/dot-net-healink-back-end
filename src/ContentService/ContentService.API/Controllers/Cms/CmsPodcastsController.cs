using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.API.DTOs;
using SharedLibrary.Commons.Services;
using ContentService.Domain.Enums;

namespace ContentService.API.Controllers.Cms;

/// <summary>
/// CMS Podcasts Controller - For administrators and moderators to manage all podcasts
/// </summary>
[ApiController]
[Route("api/cms/podcasts")]
[Authorize(Policy = "CommunityModerator")]
[ApiExplorerSettings(GroupName = "CMS")]
public class CmsPodcastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CmsPodcastsController> _logger;

    public CmsPodcastsController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CmsPodcastsController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all podcasts (admin view) with filtering
    /// </summary>
    /// <remarks>
    /// Returns all podcasts in the system regardless of status.
    /// Supports filtering by status, emotion categories, topic categories, and search terms.
    /// For moderators to review and manage content.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<GetPodcastsResponse>> GetAllPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] EmotionCategory[]? emotionCategories = null,
        [FromQuery] TopicCategory[]? topicCategories = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? seriesName = null)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: status,
            EmotionCategories: emotionCategories,
            TopicCategories: topicCategories,
            SearchTerm: searchTerm,
            SeriesName: seriesName
        );

        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "Moderator {ModeratorId} retrieved {Count} podcasts (Status: {Status})",
            _currentUserService.UserId,
            result.Podcasts.Count(),
            status?.ToString() ?? "All"
        );

        return Ok(result);
    }

    /// <summary>
    /// Get podcasts pending moderation
    /// </summary>
    /// <remarks>
    /// Returns only podcasts with PendingReview or PendingModeration status that need moderator attention.
    /// </remarks>
    [HttpGet("pending")]
    public async Task<ActionResult<GetPodcastsResponse>> GetPendingPodcasts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPodcastsQuery(
            Page: page,
            PageSize: pageSize,
            Status: ContentStatus.PendingModeration
        );

        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "Moderator {ModeratorId} retrieved {Count} pending podcasts",
            _currentUserService.UserId,
            result.Podcasts.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get a specific podcast by ID (admin view with all details)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PodcastDto>> GetPodcast(Guid id)
    {
        var query = new GetPodcastByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            _logger.LogWarning("Podcast not found: {PodcastId}", id);
            return NotFound(new { message = "Podcast not found" });
        }

        _logger.LogInformation(
            "Moderator {ModeratorId} viewed podcast: {PodcastId}",
            _currentUserService.UserId,
            id
        );

        return Ok(result);
    }

    /// <summary>
    /// Approve a podcast
    /// </summary>
    /// <remarks>
    /// Approve a podcast for publication. Changes status to Approved/Published.
    /// Only moderators and admins can approve content.
    /// </remarks>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApprovePodcastResponse>> ApprovePodcast(Guid id, [FromBody] ApprovePodcastRequest request)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var command = new ApprovePodcastCommand(id, moderatorId, request.ApprovalNotes);
        var result = await _mediator.Send(command);

        _logger.LogInformation(
            "Moderator {ModeratorId} approved podcast: {PodcastId}",
            moderatorId,
            id
        );

        return Ok(result);
    }

    /// <summary>
    /// Reject a podcast
    /// </summary>
    /// <remarks>
    /// Reject a podcast with reason. Changes status to Rejected.
    /// The content creator will be notified with the rejection reason.
    /// </remarks>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectPodcast(Guid id, [FromBody] RejectPodcastRequest request)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return BadRequest(new { message = "Rejection reason is required" });
        }

        var command = new RejectPodcastCommand(id, moderatorId, request.RejectionReason);
        var result = await _mediator.Send(command);

        if (!result)
        {
            _logger.LogWarning("Podcast not found for rejection: {PodcastId}", id);
            return NotFound(new { message = "Podcast not found" });
        }

        _logger.LogInformation(
            "Moderator {ModeratorId} rejected podcast: {PodcastId}, Reason: {Reason}",
            moderatorId,
            id,
            request.RejectionReason
        );

        return NoContent();
    }

    /// <summary>
    /// Get podcast analytics and statistics
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<ActionResult> GetPodcastAnalytics(Guid id)
    {
        var query = new GetPodcastByIdQuery(id);
        var podcast = await _mediator.Send(query);

        if (podcast == null)
        {
            return NotFound(new { message = "Podcast not found" });
        }

        // TODO: Implement comprehensive analytics
        _logger.LogInformation(
            "Moderator {ModeratorId} viewed analytics for podcast: {PodcastId}",
            _currentUserService.UserId,
            id
        );

        return Ok(new
        {
            podcastId = id,
            title = podcast.Title,
            createdBy = podcast.CreatedBy,
            status = podcast.ContentStatus.ToString(),
            viewCount = podcast.ViewCount,
            likeCount = podcast.LikeCount,
            createdAt = podcast.CreatedAt,
            publishedAt = podcast.PublishedAt,
            message = "Detailed analytics coming soon"
        });
    }

    /// <summary>
    /// Get overall content statistics (dashboard)
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        // TODO: Implement comprehensive statistics
        _logger.LogInformation(
            "Moderator {ModeratorId} accessed CMS statistics",
            _currentUserService.UserId
        );

        return Ok(new
        {
            message = "CMS statistics coming soon",
            totalPodcasts = 0,
            publishedPodcasts = 0,
            pendingPodcasts = 0,
            rejectedPodcasts = 0,
            totalViews = 0,
            totalLikes = 0
        });
    }

    /// <summary>
    /// Get moderation activity log
    /// </summary>
    [HttpGet("moderation-log")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> GetModerationLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // TODO: Implement moderation activity logging
        _logger.LogInformation(
            "Admin {AdminId} accessed moderation log",
            _currentUserService.UserId
        );

        return Ok(new
        {
            message = "Moderation log coming soon",
            activities = Array.Empty<object>()
        });
    }

    /// <summary>
    /// Bulk approve podcasts
    /// </summary>
    [HttpPost("bulk-approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> BulkApprovePodcasts([FromBody] BulkApproveRequest request)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        if (request.PodcastIds == null || !request.PodcastIds.Any())
        {
            return BadRequest(new { message = "No podcast IDs provided" });
        }

        // TODO: Implement bulk approval
        _logger.LogInformation(
            "Admin {AdminId} initiated bulk approval for {Count} podcasts",
            moderatorId,
            request.PodcastIds.Count()
        );

        return Ok(new
        {
            message = "Bulk approval feature coming soon",
            podcastIds = request.PodcastIds
        });
    }

    /// <summary>
    /// Force delete a podcast (admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> ForceDeletePodcast(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var adminId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // Use delete command but as admin (bypasses ownership check)
        var command = new DeletePodcastCommand(id, adminId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            _logger.LogWarning("Admin {AdminId} failed to delete podcast: {PodcastId}", adminId, id);
            return NotFound(new { message = "Podcast not found" });
        }

        _logger.LogWarning(
            "Admin {AdminId} force deleted podcast: {PodcastId}",
            adminId,
            id
        );

        return NoContent();
    }
}

#region Request DTOs

public class BulkApproveRequest
{
    public IEnumerable<Guid> PodcastIds { get; set; } = new List<Guid>();
    public string? ApprovalNotes { get; set; }
}

#endregion
