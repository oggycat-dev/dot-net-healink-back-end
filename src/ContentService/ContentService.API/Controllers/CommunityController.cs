using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ContentService.Application.Features.Community.Commands;
using ContentService.Application.Features.Community.Queries;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/content/community")]
public class CommunityController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get community stories with filtering
    /// </summary>
    [HttpGet("stories")]
    public async Task<ActionResult<GetCommunityStoriesResponse>> GetCommunityStories([FromQuery] GetCommunityStoriesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new community story (Community Member only)
    /// </summary>
    [HttpPost("stories")]
    [Authorize] // Add community member role check
    public async Task<ActionResult<CreateCommunityStoryResponse>> CreateCommunityStory([FromBody] CreateCommunityStoryCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCommunityStories), new { id = result.Id }, result);
    }

    /// <summary>
    /// Approve a community story (Moderator only)
    /// </summary>
    [HttpPost("stories/{id}/approve")]
    [Authorize] // Add moderator role check
    public async Task<ActionResult<ApproveCommunityStoryResponse>> ApproveCommunityStory(Guid id, [FromBody] ApproveCommunityStoryCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Reject a community story (Moderator only)
    /// </summary>
    [HttpPost("stories/{id}/reject")]
    [Authorize] // Add moderator role check
    public async Task<ActionResult> RejectCommunityStory(Guid id, [FromBody] RejectCommunityStoryCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        return result ? Ok() : NotFound();
    }

    /// <summary>
    /// Mark story as helpful
    /// </summary>
    [HttpPost("stories/{id}/helpful")]
    [Authorize]
    public async Task<ActionResult> MarkAsHelpful(Guid id)
    {
        // Implementation will be added with interaction handlers
        return Ok(new { message = "Marked as helpful" });
    }
}