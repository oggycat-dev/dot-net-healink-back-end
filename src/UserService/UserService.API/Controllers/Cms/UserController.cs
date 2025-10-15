using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;
using UserService.Application.Commons.DTOs;
using UserService.Application.Features.Profile.Queries.GetProfile;
using UserService.Application.Features.Users.Commands.CreateUserByAdmin;
using UserService.Application.Features.Users.Queries.GetUsers;
using UserService.Application.Features.Users.Queries.GetUserById;


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
    /// Create new user by admin (Admin or Staff role required)
    /// Sample request:
    /// 
    ///     POST /api/cms/users
    ///     {
    ///         "email": "newuser@example.com",
    ///         "password": "SecurePass123!",
    ///         "fullName": "John Doe",
    ///         "phoneNumber": "+84901234567",
    ///         "address": "123 Street, City",
    ///         "role": 1
    ///     }
    /// 
    /// Role values: 0=Admin, 1=Staff, 2=User, 3=ContentCreator
    /// </summary>
    /// <param name="command">User creation request</param>
    /// <returns>Created user profile</returns>
    /// <response code="200">User created successfully</response>
    /// <response code="400">Validation error (email exists, invalid role, etc.)</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">No access (requires Admin or Staff role)</response>
    [HttpPost]
    [AuthorizeRoles("Admin")]
    [ProducesResponseType(typeof(Result<CreateUserByAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreateUserByAdminResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CreateUserByAdminResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<CreateUserByAdminResponse>), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Create new user by admin",
        Description = "Admin or Staff creates a new user with specified role. Triggers saga for user creation workflow.",
        OperationId = "CreateUserByAdmin",
        Tags = new[] { "CMS", "CMS_User" }
    )]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminCommand command)
    {
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
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
    
    /// <summary>
    /// Get paginated list of users with dynamic filters
    /// Roles are fetched from AuthService via RPC for each page
    /// Sample request:
    /// 
    ///     GET /api/cms/users?page=1&amp;pageSize=10&amp;search=john&amp;sortBy=createdAt&amp;isAscending=false
    ///     GET /api/cms/users?page=1&amp;pageSize=20&amp;email=test@example.com&amp;status=0
    ///     GET /api/cms/users?page=1&amp;pageSize=10&amp;lastLoginFrom=2024-01-01&amp;lastLoginTo=2024-12-31
    /// 
    /// </summary>
    /// <param name="filter">Dynamic filter parameters</param>
    /// <returns>Paginated list of users with roles</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">No access (requires Admin or Staff role)</response>
    [HttpGet]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(PaginationResult<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<UserProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<UserProfileResponse>), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get paginated users with dynamic filters",
        Description = "Retrieves paginated users with dynamic filtering. Roles are fetched from AuthService via RPC using Task.WhenAll for concurrent processing.",
        OperationId = "GetUsers",
        Tags = new[] { "CMS", "CMS_User" }
    )]
    public async Task<IActionResult> GetUsers([FromQuery] UserProfileFilter filter)
    {
        var query = new GetUsersQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
    
    /// <summary>
    /// Get user by ID with roles from AuthService
    /// Sample request:
    /// 
    ///     GET /api/cms/users/{id}
    /// 
    /// </summary>
    /// <param name="id">User profile ID</param>
    /// <returns>User profile with roles</returns>
    /// <response code="200">User retrieved successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Not authorized</response>
    /// <response code="403">No access (requires Admin or Staff role)</response>
    [HttpGet("{id:guid}")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserProfileResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<UserProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<UserProfileResponse>), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Get user by ID",
        Description = "Retrieves a single user by ID. Roles are fetched from AuthService via RPC.",
        OperationId = "GetUserById",
        Tags = new[] { "CMS", "CMS_User" }
    )]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}