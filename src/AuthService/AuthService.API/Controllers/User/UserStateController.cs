using AuthService.Application.Commons.DTOs;
using AuthService.Application.Features.Auth.Queries.GetUserState;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace AuthService.API.Controllers.User;

/// <summary>
/// Controller quản lý user state cho website User
/// </summary>
[ApiController]
[Route("api/user/state")]
[ApiExplorerSettings(GroupName = "v1")]
[SharedLibrary.Commons.Configurations.Tags("User", "User_State")]
[SwaggerTag("This API is used for getting user state information")]
public class UserStateController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserStateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current user state from cache with fallback to identity service
    /// </summary>
    /// <remarks>
    /// This API is used for getting current user state information including roles, subscription status, and content creator status.
    /// It first tries to get data from Redis cache, and if not available, falls back to identity service.
    /// This is the single source of truth for content creator status.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/user/state/me
    /// 
    /// Headers:
    ///     Authorization: Bearer &lt;access_token&gt;
    /// </remarks>
    /// <returns>User state information</returns>
    /// <response code="200">User state retrieved successfully</response>
    /// <response code="401">Unauthorized (invalid or expired token)</response>
    /// <response code="403">Forbidden (user not found or inactive)</response>
    [HttpGet("me")]
    [AuthorizeRoles("User")]
    [ProducesResponseType(typeof(Result<UserStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get current user state",
        Description = "Get current user state information from cache with fallback to identity service",
        OperationId = "GetUserState",
        Tags = new[] { "User", "User_State" }
    )]
    public async Task<IActionResult> GetUserState()
    {
        var query = new GetUserStateQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Check if current user is a content creator
    /// </summary>
    /// <remarks>
    /// This API is used for checking if the current user is a content creator.
    /// It uses the same cache-first approach as the main user state endpoint.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/user/state/is-content-creator
    /// 
    /// Headers:
    ///     Authorization: Bearer &lt;access_token&gt;
    /// </remarks>
    /// <returns>Content creator status</returns>
    /// <response code="200">Content creator status retrieved successfully</response>
    /// <response code="401">Unauthorized (invalid or expired token)</response>
    /// <response code="403">Forbidden (user not found or inactive)</response>
    [HttpGet("is-content-creator")]
    [AuthorizeRoles("User")]
    [ProducesResponseType(typeof(Result<ContentCreatorStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Check if user is content creator",
        Description = "Check if the current user is a content creator using cache-first approach",
        OperationId = "IsContentCreator",
        Tags = new[] { "User", "User_State" }
    )]
    public async Task<IActionResult> IsContentCreator()
    {
        var query = new IsContentCreatorQuery();
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        
        return Ok(result);
    }
}
