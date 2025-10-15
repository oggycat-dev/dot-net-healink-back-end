using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Community.Commands;
using ContentService.Application.Features.Community.Queries;
using SharedLibrary.Commons.Services;

namespace ContentService.API.Controllers.Creator;

/// <summary>
/// Creator Community Controller - For community members to create and manage their stories
/// </summary>
[ApiController]
[Route("api/creator/community")]
[Authorize]
[ApiExplorerSettings(GroupName = "Creator")]
public class CreatorCommunityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatorCommunityController> _logger;

    public CreatorCommunityController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CreatorCommunityController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get my community stories
    /// </summary>
    [HttpGet("my-stories")]
    public async Task<ActionResult<GetCommunityStoriesResponse>> GetMyStories([FromQuery] GetCommunityStoriesQuery query)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // TODO: Filter by userId in query
        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "User {UserId} retrieved their community stories: {Count} results",
            userId,
            result.Stories.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Create a new community story
    /// </summary>
    [HttpPost("stories")]
    public async Task<ActionResult<CreateCommunityStoryResponse>> CreateStory([FromBody] CreateCommunityStoryCommand command)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        var result = await _mediator.Send(command);

        _logger.LogInformation(
            "User {UserId} created community story: {StoryId}",
            userId,
            result.Id
        );

        return CreatedAtAction(nameof(GetMyStories), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update my community story
    /// </summary>
    [HttpPut("stories/{id}")]
    public async Task<ActionResult> UpdateStory(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // TODO: Implement UpdateCommunityStoryCommand
        _logger.LogInformation("User {UserId} updated story: {StoryId}", userId, id);

        return Ok(new { message = "Update story coming soon", storyId = id });
    }

    /// <summary>
    /// Delete my community story
    /// </summary>
    [HttpDelete("stories/{id}")]
    public async Task<ActionResult> DeleteStory(Guid id)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Unauthorized(new { message = "Invalid user ID" });
        }

        // TODO: Implement DeleteCommunityStoryCommand
        _logger.LogInformation("User {UserId} deleted story: {StoryId}", userId, id);

        return NoContent();
    }
}
