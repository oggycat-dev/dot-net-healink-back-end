using Microsoft.AspNetCore.Mvc;
using MediatR;
using ContentService.Application.Events;
using SharedLibrary.Commons.EventBus;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IEventBus _eventBus;

    public HealthController(ILogger<HealthController> logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "ContentService",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            features = new[]
            {
                "Content Management",
                "Podcast Creation", 
                "Community Stories",
                "RabbitMQ Integration",
                "Event Publishing"
            }
        });
    }

    [HttpPost("test-event")]
    public async Task<IActionResult> TestEventPublish()
    {
        try
        {
            // Test event publishing
            var testEvent = new PodcastCreatedEvent(
                Guid.NewGuid(),
                "Test RabbitMQ Podcast",
                "This is a test podcast to verify RabbitMQ event publishing",
                "https://example.com/test-audio.mp3",
                TimeSpan.FromMinutes(15),
                Guid.NewGuid(), // Test user ID
                DateTime.UtcNow,
                new[] { "test", "rabbitmq" },
                new[] { ContentService.Domain.Enums.EmotionCategory.Happiness },
                new[] { ContentService.Domain.Enums.TopicCategory.MentalHealth }
            );

            await _eventBus.PublishAsync(testEvent);

            _logger.LogInformation("Test event published successfully: {EventId}", testEvent.Id);

            return Ok(new
            {
                success = true,
                message = "Test event published to RabbitMQ successfully!",
                eventId = testEvent.Id,
                eventType = "PodcastCreatedEvent",
                publishedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test event");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish test event",
                error = ex.Message
            });
        }
    }
}