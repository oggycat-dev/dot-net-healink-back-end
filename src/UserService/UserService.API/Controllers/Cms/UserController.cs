using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;
using UserService.Application.Commons.DTOs;
using UserService.Application.Features.Profile.Queries.GetProfile;

namespace UserService.API.Controllers.Cms;

/// <summary>
/// Controller quản lý user cho website CMS
/// </summary>
[ApiController]
[Route("api/cms/users")]
[ApiExplorerSettings(GroupName = "v1")]
[SharedLibrary.Commons.Configurations.Tags("CMS", "CMS_User")]
[SwaggerTag("This API is used for User for CMS website")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Get profile of user in CMS website
    /// Need to be authenticated and have Admin or Staff role
    /// Sample request:
    /// 
    ///     GET /api/cms/user/profile
    /// 
    /// Headers:
    ///     Authorization: Bearer &lt;access_token&gt;
    /// </summary>
    /// <returns>Profile of user in CMS website</returns>
    /// <response code="200">Get profile successfully</response>
    /// <response code="401">Get profile failed (not authorized)</response>
    /// <response code="400">Get profile failed (validation error)</response>
    /// <response code="403">Get profile failed (no access)</response>
    [HttpGet("profile")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get profile of user in CMS website",
        Description = "Get profile of user in CMS website",
        OperationId = "GetProfile",
        Tags = new[] { "CMS", "CMS_User" }
    )]
    public async Task<IActionResult> GetProfile()
    {
        var query = new GetProfileQuery();
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}