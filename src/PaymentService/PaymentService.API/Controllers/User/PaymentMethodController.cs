using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commons.DTOs;
using PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethodById;
using PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethods;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PaymentService.API.Controllers.User;

/// <summary>
/// Controller for viewing payment methods (User)
/// </summary>
[ApiController]
[Route("api/user/payment-methods")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for viewing available payment methods")]
[SharedLibrary.Commons.Configurations.Tags("User", "User_PaymentMethod")]
public class PaymentMethodController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentMethodController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active payment methods (public - no auth required)
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>List of active payment methods</returns>
    /// <response code="200">Success</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get active payment methods",
        Description = "Retrieve all active payment methods available for transactions (public endpoint)",
        OperationId = "GetActivePaymentMethods",
        Tags = new[] { "User", "User_PaymentMethod" }
    )]
    public async Task<IActionResult> GetPaymentMethods([FromQuery] PaymentMethodFilter filter)
    {
        // Note: PaymentMethod doesn't have status field
        // Active payment methods are determined by IsDeleted field
        
        var query = new GetPaymentMethodsQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get payment method by ID (public - no auth required)
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <returns>Payment method details</returns>
    /// <response code="200">Success</response>
    /// <response code="404">Not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get payment method by ID",
        Description = "Retrieve a single active payment method by its unique identifier",
        OperationId = "GetPaymentMethodByIdUser",
        Tags = new[] { "User", "User_PaymentMethod" }
    )]
    public async Task<IActionResult> GetPaymentMethodById(Guid id)
    {
        var query = new GetPaymentMethodByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}

