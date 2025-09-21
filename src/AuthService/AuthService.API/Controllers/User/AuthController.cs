using AuthService.Application.Commons.DTOs;
using AuthService.Application.Features.Auth.Commands.Login;
using AuthService.Application.Features.Auth.Commands.Logout;
using AuthService.Application.Features.Auth.Commands.Register;
using AuthService.Application.Features.Auth.Commands.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace AuthService.API.Controllers.User;

/// <summary>
/// Controller quản lý xác thực cho website User
/// </summary>
[ApiController]
[Route("api/user/auth")]
[ApiExplorerSettings(GroupName = "v1")]
[SharedLibrary.Commons.Configurations.Tags("User", "User_Auth")]
[SwaggerTag("This API is used for Authentication for User website")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register new user - starts the registration saga
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Register new user",
        Description = "Starts the registration workflow with OTP verification",
        OperationId = "Register",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<ActionResult<Result>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RegisterCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Verify OTP - continues the registration saga
    /// </summary>
    /// <param name="request">OTP verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Verify OTP",
        Description = "Verifies OTP for user registration or password reset",
        OperationId = "VerifyOtp", 
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<ActionResult<Result>> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new VerifyOtpCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Login to the User system
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/user/auth/login
    ///     {
    ///        "email": "user@example.com",
    ///        "password": "User@123",
    ///        "grant_type": 0
    ///     }
    ///     
    /// `grant_type` default is 0 (Password)
    /// </remarks>
    /// <param name="request">Login request</param>
    /// <returns>User information and authentication token</returns>
    /// <response code="200">Login successfully</response>
    /// <response code="400">Login failed (validation error)</response>
    /// <response code="401">Login failed (email or password is incorrect)</response>
    /// <response code="403">No access (user is not a User member)</response>
    [HttpPost("login")]
    [ServiceFilter(typeof(UserRoleAccessFilter))]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Login to the User system",
        Description = "This API is used for Authentication for User website",
        OperationId = "Login",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Logout from the User system
    /// </summary>
    /// <remarks>
    /// This API is used for Logging out from the User website. It will clear the refresh token and refresh token expiry time in the database.
    /// Need access token in the header.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/user/auth/logout
    /// 
    /// Headers:
    ///     Authorization: Bearer &lt;access_token&gt;
    /// </remarks>
    /// <returns>Logout successfully</returns>
    /// <response code="200">Logout successfully</response>
    /// <response code="401">Logout failed (not authorized)</response>
    /// <response code="403">No access (user is not a User member)</response>
    [HttpPost("logout")]
    [AuthorizeRoles("User")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Logout from the User system",
        Description = "This API is used for Logging out from the User website",
        OperationId = "Logout",
        Tags = new[] { "User", "User_Auth" }
    )]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand();
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }
        return Ok(result);
    }
}