using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.API.Controllers;


/// <summary>
/// Health check controller for User Service
/// </summary>
[ApiController]
[Route("")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("Health check endpoints for User Service")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check for User Service
    /// </summary>
    /// <returns>Service health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Health check for User Service",
        Description = "Check if User Service is running and healthy",
        OperationId = "UserHealthCheck",
        Tags = new[] { "Health" }
    )]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "UserService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            features = new[]
            {
                "Users Management",
                "Event Bus Integration",
                "Outbox Pattern"
            }
        });
    }
}
