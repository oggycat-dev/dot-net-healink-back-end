using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commons.DTOs;
using PaymentService.Application.Features.PaymentMethods.Commands.CreatePaymentMethod;
using PaymentService.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;
using PaymentService.Application.Features.PaymentMethods.Commands.UpdatePaymentMethod;
using PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethodById;
using PaymentService.Application.Features.PaymentMethods.Queries.GetPaymentMethods;
using SharedLibrary.Commons.Attributes;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PaymentService.API.Controllers.Cms;

/// <summary>
/// Controller for managing payment methods in CMS
/// </summary>
[ApiController]
[Route("api/cms/payment-methods")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("This API is used for managing payment methods in CMS")]
[SharedLibrary.Commons.Configurations.Tags("CMS", "CMS_PaymentMethod")]
public class PaymentMethodController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentMethodController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all payment methods with filters and pagination
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of payment methods</returns>
    /// <response code="200">Success</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginationResult<PaymentMethodResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all payment methods",
        Description = "Retrieve paginated payment methods with optional filters",
        OperationId = "GetPaymentMethods",
        Tags = new[] { "CMS", "CMS_PaymentMethod" }
    )]
    public async Task<IActionResult> GetPaymentMethods([FromQuery] PaymentMethodFilter filter)
    {
        var query = new GetPaymentMethodsQuery(filter);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Get payment method by ID
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <returns>Payment method details</returns>
    /// <response code="200">Success</response>
    /// <response code="404">Not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("{id}")]
    [AuthorizeRoles("Admin", "Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<PaymentMethodResponse>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get payment method by ID",
        Description = "Retrieve a single payment method by its unique identifier",
        OperationId = "GetPaymentMethodById",
        Tags = new[] { "CMS", "CMS_PaymentMethod" }
    )]
    public async Task<IActionResult> GetPaymentMethodById(Guid id)
    {
        var query = new GetPaymentMethodByIdQuery(id);
        var result = await _mediator.Send(query);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Create a new payment method
    /// </summary>
    /// <param name="request">Payment method creation request</param>
    /// <returns>Created payment method</returns>
    /// <response code="200">Created successfully</response>
    /// <response code="400">Bad request (validation error)</response>
    /// <response code="409">Conflict (method already exists)</response>
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
        Summary = "Create payment method",
        Description = "Create a new payment method with specified details",
        OperationId = "CreatePaymentMethod",
        Tags = new[] { "CMS", "CMS_PaymentMethod" }
    )]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] PaymentMethodRequest request)
    {
        var command = new CreatePaymentMethodCommand(request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Update an existing payment method
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated payment method</returns>
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
        Summary = "Update payment method",
        Description = "Update an existing payment method",
        OperationId = "UpdatePaymentMethod",
        Tags = new[] { "CMS", "CMS_PaymentMethod" }
    )]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, [FromBody] PaymentMethodRequest request)
    {
        var command = new UpdatePaymentMethodCommand(id, request);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    /// <summary>
    /// Delete a payment method
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Deleted successfully</response>
    /// <response code="400">Bad request (has associated transactions)</response>
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
        Summary = "Delete payment method",
        Description = "Soft delete a payment method (fails if there are associated transactions)",
        OperationId = "DeletePaymentMethod",
        Tags = new[] { "CMS", "CMS_PaymentMethod" }
    )]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var command = new DeletePaymentMethodCommand(id);
        var result = await _mediator.Send(command);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}

