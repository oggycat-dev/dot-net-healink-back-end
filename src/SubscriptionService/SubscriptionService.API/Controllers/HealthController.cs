using Microsoft.AspNetCore.Mvc;

namespace SubscriptionService.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { Status = "Healthy", Service = "SubscriptionService", Timestamp = DateTime.UtcNow });
    }
}
