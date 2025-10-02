using Microsoft.AspNetCore.Mvc;

namespace PaymentService.API.Controllers;

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
        return Ok(new { Status = "Healthy", Service = "PaymentService", Timestamp = DateTime.UtcNow });
    }
}