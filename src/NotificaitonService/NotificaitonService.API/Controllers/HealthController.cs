using Microsoft.AspNetCore.Mvc;

namespace NotificaitonService.API.Controllers;

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
        return Ok(new { Status = "Healthy", Service = "NotificationService", Timestamp = DateTime.UtcNow });
    }
}
