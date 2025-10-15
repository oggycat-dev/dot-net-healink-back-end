using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;
using SubscriptionService.Application.Features.SubscriptionPlans.Commands.DeleteSubscriptionPlan;
using SubscriptionService.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;
using SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanById;
using SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;
using Swashbuckle.AspNetCore.Annotations;

namespace SubscriptionService.API.Controllers.Cms;

/// <summary>
/// Controller for managing subscription plans in CMS
/// </summary>
[ApiController]
[Route("api/cms/subscription-plans")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for managing subscription plans in CMS")]
[SharedLibrary.Commons.Configurations.Tags("CMS", "CMS_SubscriptionPlan")]
public class SubscriptionPlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionPlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all subscription plans with filters and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of subscription plans</returns>
    /// <response code="200">Success</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all subscription plans",
        Description = "Retrieve paginated subscription plans with optional filters",
        OperationId = "GetSubscriptionPlans",
        Tags = new[] { "CMS", "CMS_SubscriptionPlan" }
    )]
    public async Task<IActionResult> GetSubscriptionPlans([FromQuery] SubscriptionPlanFilter filter)
    {
        var query = new GetSubscriptionPlansQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <returns>Subscription plan details</returns>
    /// <response code="200">Success</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("{id}")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get subscription plan by ID",
        Description = "Retrieve a single subscription plan by its unique identifier",
        OperationId = "GetSubscriptionPlanById",
        Tags = new[] { "CMS", "CMS_SubscriptionPlan" }
    )]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid id)
    {
        var query = new GetSubscriptionPlanByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Create a new subscription plan
    /// </summary>
    /// <param name="request">Subscription plan creation request</param>
    /// <returns>Created subscription plan</returns>
    /// <response code="201">Created successfully</response>
    /// <response code="400">Bad request (validation error)</response>
    /// <response code="409">Conflict (plan already exists)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpPost]
    [AuthorizeRoles("Admin")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result),StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Create subscription plan",
        Description = "Create a new subscription plan with specified details",
        OperationId = "CreateSubscriptionPlan",
        Tags = new[] { "CMS", "CMS_SubscriptionPlan" }
    )]
    public async Task<IActionResult> CreateSubscriptionPlan([FromBody] SubscriptionPlanRequest request)
    {
        var command = new CreateSubscriptionPlanCommand(request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Update an existing subscription plan
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated subscription plan</returns>
    /// <response code="200">Updated successfully</response>
    /// <response code="400">Bad request (validation error)</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpPut("{id}")]
    [AuthorizeRoles("Admin")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update subscription plan",
        Description = "Update an existing subscription plan (partial update supported)",
        OperationId = "UpdateSubscriptionPlan",
        Tags = new[] { "CMS", "CMS_SubscriptionPlan" }
    )]
    public async Task<IActionResult> UpdateSubscriptionPlan(Guid id, [FromBody] SubscriptionPlanRequest request)
    {
        var command = new UpdateSubscriptionPlanCommand(id, request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Delete a subscription plan
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Deleted successfully</response>
    /// <response code="400">Bad request (has active subscriptions)</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpDelete("{id}")]
    [AuthorizeRoles("Admin")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Delete subscription plan",
        Description = "Soft delete a subscription plan (fails if there are active subscriptions)",
        OperationId = "DeleteSubscriptionPlan",
        Tags = new[] { "CMS", "CMS_SubscriptionPlan" }
    )]
    public async Task<IActionResult> DeleteSubscriptionPlan(Guid id)
    {
        var command = new DeleteSubscriptionPlanCommand(id);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}
