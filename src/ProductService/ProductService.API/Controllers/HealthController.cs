using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductAuthMicroservice.ProductService.API.Controllers;

/// <summary>
/// Health check controller for Product Service
/// </summary>
[ApiController]
[Route("")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("Health check endpoints for Product Service")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check for Product Service
    /// </summary>
    /// <returns>Service health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Health check for Product Service",
        Description = "Check if Product Service is running and healthy",
        OperationId = "ProductHealthCheck",
        Tags = new[] { "Health" }
    )]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "ProductService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            features = new[]
            {
                "Products CRUD",
                "Categories CRUD", 
                "Product Inventory Management",
                "Event Bus Integration",
                "Outbox Pattern"
            }
        });
    }
}
