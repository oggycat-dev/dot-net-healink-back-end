using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Features.Subscriptions.Commands.CancelSubscription;
using SubscriptionService.Application.Features.Subscriptions.Commands.UpdateSubscription;
using SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptionById;
using SubscriptionService.Application.Features.Subscriptions.Queries.GetSubscriptions;
using Swashbuckle.AspNetCore.Annotations;

namespace SubscriptionService.API.Controllers.Cms;

/// <summary>
/// Controller for managing user subscriptions in CMS
/// </summary>
[ApiController]
[Route("api/cms/subscriptions")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for managing user subscriptions in CMS")]
[SharedLibrary.Commons.Configurations.Tags("CMS", "CMS_Subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all subscriptions with filters and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of subscriptions</returns>
    /// <response code="200">Success</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all subscriptions",
        Description = "Retrieve paginated subscriptions with optional filters",
        OperationId = "GetSubscriptions",
        Tags = new[] { "CMS", "CMS_Subscription" }
    )]
    public async Task<IActionResult> GetSubscriptions([FromQuery] SubscriptionFilter filter)
    {
        var query = new GetSubscriptionsQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <returns>Subscription details</returns>
    /// <response code="200">Success</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("{id}")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get subscription by ID",
        Description = "Retrieve a single subscription by its unique identifier",
        OperationId = "GetSubscriptionById",
        Tags = new[] { "CMS", "CMS_Subscription" }
    )]
    public async Task<IActionResult> GetSubscriptionById(Guid id)
    {
        var query = new GetSubscriptionByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Update subscription settings
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated subscription</returns>
    /// <response code="200">Updated successfully</response>
    /// <response code="400">Bad request (validation error)</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpPut("{id}")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update subscription",
        Description = "Update subscription settings (status, renewal behavior, etc.)",
        OperationId = "UpdateSubscription",
        Tags = new[] { "CMS", "CMS_Subscription" }
    )]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        var command = new UpdateSubscriptionCommand(id, request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="cancelAtPeriodEnd">Whether to cancel at period end (default: true)</param>
    /// <param name="reason">Cancellation reason (optional)</param>
    /// <returns>Cancelled subscription</returns>
    /// <response code="200">Cancelled successfully</response>
    /// <response code="400">Bad request (already cancelled)</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpPost("{id}/cancel")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Cancel subscription",
        Description = "Cancel a subscription immediately or at period end",
        OperationId = "CancelSubscription",
        Tags = new[] { "CMS", "CMS_Subscription" }
    )]
    public async Task<IActionResult> CancelSubscription(
        Guid id,
        [FromQuery] bool cancelAtPeriodEnd = true,
        [FromQuery] string? reason = null)
    {
        var command = new CancelSubscriptionCommand(id, cancelAtPeriodEnd, reason);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}
