using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Community.Commands;
using ContentService.Application.Features.Community.Queries;
using SharedLibrary.Commons.Services;

namespace ContentService.API.Controllers.Cms;

/// <summary>
/// CMS Community Controller - For moderators to manage community stories
/// </summary>
[ApiController]
[Route("api/cms/community")]
[Authorize(Policy = "CommunityModerator")]
[ApiExplorerSettings(GroupName = "CMS")]
public class CmsCommunityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CmsCommunityController> _logger;

    public CmsCommunityController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CmsCommunityController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all community stories (admin view)
    /// </summary>
    [HttpGet("stories")]
    public async Task<ActionResult<GetCommunityStoriesResponse>> GetAllStories([FromQuery] GetCommunityStoriesQuery query)
    {
        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "Moderator {ModeratorId} retrieved {Count} community stories",
            _currentUserService.UserId,
            result.Stories.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get pending community stories for moderation
    /// </summary>
    [HttpGet("stories/pending")]
    public async Task<ActionResult<GetCommunityStoriesResponse>> GetPendingStories([FromQuery] GetCommunityStoriesQuery query)
    {
        // TODO: Add status filter for pending
        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "Moderator {ModeratorId} retrieved pending community stories",
            _currentUserService.UserId
        );

        return Ok(result);
    }

    /// <summary>
    /// Approve a community story
    /// </summary>
    [HttpPost("stories/{id}/approve")]
    public async Task<ActionResult<ApproveCommunityStoryResponse>> ApproveStory(Guid id, [FromBody] ApproveCommunityStoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        if (!Guid.TryParse(_currentUserService.UserId, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var result = await _mediator.Send(command);

        _logger.LogInformation(
            "Moderator {ModeratorId} approved community story: {StoryId}",
            moderatorId,
            id
        );

        return Ok(result);
    }

    /// <summary>
    /// Reject a community story
    /// </summary>
    [HttpPost("stories/{id}/reject")]
    public async Task<ActionResult> RejectStory(Guid id, [FromBody] RejectCommunityStoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        if (!Guid.TryParse(_currentUserService.UserId, out var moderatorId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound(new { message = "Story not found" });
        }

        _logger.LogInformation(
            "Moderator {ModeratorId} rejected community story: {StoryId}",
            moderatorId,
            id
        );

        return NoContent();
    }

    /// <summary>
    /// Get community statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        _logger.LogInformation(
            "Moderator {ModeratorId} accessed community statistics",
            _currentUserService.UserId
        );

        return Ok(new
        {
            message = "Community statistics coming soon",
            totalStories = 0,
            approvedStories = 0,
            pendingStories = 0,
            rejectedStories = 0
        });
    }
}
