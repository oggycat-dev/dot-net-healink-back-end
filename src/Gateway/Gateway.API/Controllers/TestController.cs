using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "Gateway API is working!",
            timestamp = DateTime.UtcNow
        });
    }
}
