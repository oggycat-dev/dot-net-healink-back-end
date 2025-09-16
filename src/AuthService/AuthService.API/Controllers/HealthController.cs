using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductAuthMicroservice.AuthService.API.Controllers;

/// <summary>
/// Health check controller for Auth Service
/// </summary>
[ApiController]
[Route("")]
[ApiExplorerSettings(GroupName = "v1")]
[SwaggerTag("Health check endpoints for Auth Service")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check for Auth Service
    /// </summary>
    /// <returns>Service health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Health check for Auth Service",
        Description = "Check if Auth Service is running and healthy",
        OperationId = "AuthHealthCheck",
        Tags = new[] { "Health" }
    )]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "AuthService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            features = new[]
            {
                "User Authentication",
                "JWT Token Management",
                "User Validation",
                "Role Management",
                "User Action Logging",
                "Event Handling"
            }
        });
    }
}
