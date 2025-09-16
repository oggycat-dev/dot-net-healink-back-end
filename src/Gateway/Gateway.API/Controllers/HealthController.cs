using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductAuthMicroservice.Gateway.API.Controllers;

/// <summary>
/// Health check controller for API Gateway
/// </summary>
[ApiController]
[Route("")]
[ApiExplorerSettings(GroupName = "v1")]
// [SwaggerTag("Health check endpoints for API Gateway")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check for API Gateway
    /// </summary>
    /// <returns>Service health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    // [SwaggerOperation(
    //     Summary = "Health check for API Gateway",
    //     Description = "Check if API Gateway is running and healthy",
    //     OperationId = "GatewayHealthCheck",
    //     Tags = new[] { "Health" }
    // )]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "Gateway",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            features = new[]
            {
                "API Gateway Routing",
                "JWT Authentication",
                "Distributed Authorization",
                "Load Balancing",
                "Service Discovery",
                "CORS Support"
            }
        });
    }
}
