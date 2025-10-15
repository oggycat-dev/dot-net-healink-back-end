using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlanById;
using SubscriptionService.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;
using Swashbuckle.AspNetCore.Annotations;

namespace SubscriptionService.API.Controllers.User;

/// <summary>
/// Controller for viewing subscription plans (User)
/// </summary>
[ApiController]
[Route("api/user/subscription-plans")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for viewing available subscription plans")]
[SharedLibrary.Commons.Configurations.Tags("User", "User_SubscriptionPlan")]
public class SubscriptionPlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionPlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active subscription plans (public - no auth required)
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>List of active subscription plans</returns>
    /// <response code="200">Success</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<SubscriptionPlanResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get active subscription plans",
        Description = "Retrieve all active subscription plans available for purchase (public endpoint)",
        OperationId = "GetActiveSubscriptionPlans",
        Tags = new[] { "User", "User_SubscriptionPlan" }
    )]
    public async Task<IActionResult> GetSubscriptionPlans([FromQuery] SubscriptionPlanFilter filter)
    {
        // Force status to Active for user endpoint
        filter.Status = SharedLibrary.Commons.Enums.EntityStatusEnum.Active;
        
        var query = new GetSubscriptionPlansQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get subscription plan by ID (public - no auth required)
    /// </summary>
    /// <param name="id">Subscription plan ID</param>
    /// <returns>Subscription plan details</returns>
    /// <response code="200">Success</response>
    /// <response code="404">Not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<SubscriptionPlanResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get subscription plan by ID",
        Description = "Retrieve a single active subscription plan by its unique identifier",
        OperationId = "GetSubscriptionPlanByIdUser",
        Tags = new[] { "User", "User_SubscriptionPlan" }
    )]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid id)
    {
        var query = new GetSubscriptionPlanByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}
