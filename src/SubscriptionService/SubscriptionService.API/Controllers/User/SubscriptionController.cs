using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;
using SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptionById;
using SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptions;
using SubscriptionService.Application.Features.Subscriptions.Queries.GetMySubscription;
using Swashbuckle.AspNetCore.Annotations;

namespace SubscriptionService.API.Controllers.User;

/// <summary>
/// Controller for user subscription management
/// </summary>
[ApiController]
[Route("api/user/subscriptions")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for user subscription management")]
[SharedLibrary.Commons.Configurations.Tags("User", "User_Subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new subscription (requires authentication)
    /// Returns payment URL/QR code for immediate redirect
    /// </summary>
    /// <param name="request">Subscription registration request</param>
    /// <returns>Payment intent data (PaymentUrl, QrCodeBase64, etc.)</returns>
    /// <response code="200">Success - Payment intent created</response>
    /// <response code="400">Bad request (validation error)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Conflict (already has active subscription)</response>
    [HttpPost("register")]
    [AuthorizeRoles("User")]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Register subscription",
        Description = "Register for a new subscription plan. Returns payment URL and QR code for immediate payment.",
        OperationId = "RegisterSubscription",
        Tags = new[] { "User", "User_Subscription" }
    )]
    public async Task<IActionResult> RegisterSubscription([FromBody] RegisterSubscriptionRequest request)
    {
        var command = new RegisterSubscriptionCommand(request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get current user's active subscription (requires authentication)
    /// Uses UserProfileId from cache - no direct ID access
    /// </summary>
    /// <returns>User's active subscription</returns>
    /// <response code="200">Success</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Not found (no active subscription)</response>
    [HttpGet("me")]
    [AuthorizeRoles("User")]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<SubscriptionResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get my subscription",
        Description = "Retrieve current user's active subscription. Uses UserProfileId from session cache.",
        OperationId = "GetMySubscription",
        Tags = new[] { "User", "User_Subscription" }
    )]
    public async Task<IActionResult> GetMySubscription()
    {
        var query = new GetMySubscriptionQuery();
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

}
