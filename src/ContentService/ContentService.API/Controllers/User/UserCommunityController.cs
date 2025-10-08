using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Community.Queries;

namespace ContentService.API.Controllers.User;

/// <summary>
/// User Community Controller - For regular users to browse community stories
/// </summary>
[ApiController]
[Route("api/user/community")]
[ApiExplorerSettings(GroupName = "User")]
public class UserCommunityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserCommunityController> _logger;

    public UserCommunityController(IMediator mediator, ILogger<UserCommunityController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get approved community stories (Public access)
    /// </summary>
    /// <remarks>
    /// Returns only approved and published community stories that are visible to all users.
    /// </remarks>
    [HttpGet("stories")]
    [AllowAnonymous]
    public async Task<ActionResult<GetCommunityStoriesResponse>> GetApprovedStories([FromQuery] GetCommunityStoriesQuery query)
    {
        // Only show approved stories to regular users
        // TODO: Add status filter to query if not already present
        var result = await _mediator.Send(query);

        _logger.LogInformation(
            "User retrieved {Count} approved community stories",
            result.Stories.Count()
        );

        return Ok(result);
    }

    /// <summary>
    /// Get a specific community story by ID
    /// </summary>
    [HttpGet("stories/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetStory(Guid id)
    {
        // TODO: Implement GetCommunityStoryByIdQuery
        _logger.LogInformation("User viewed community story: {StoryId}", id);

        return Ok(new { message = "Get story by ID coming soon", storyId = id });
    }

    /// <summary>
    /// Get trending community stories
    /// </summary>
    [HttpGet("stories/trending")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTrendingStories([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("User retrieved trending community stories");

        return Ok(new { message = "Trending stories coming soon" });
    }

    /// <summary>
    /// Search community stories
    /// </summary>
    [HttpGet("stories/search")]
    [AllowAnonymous]
    public async Task<ActionResult> SearchStories([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { message = "Search keyword is required" });
        }

        _logger.LogInformation("User searched community stories with keyword: {Keyword}", keyword);

        return Ok(new { message = "Story search coming soon", keyword });
    }
}
