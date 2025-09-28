using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.SharedLibrary.Contracts.Events;
using SharedLibrary.Commons.Enums;
using ContentService.Application.Events;
using ContentService.Domain.Enums;

namespace ContentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventTestController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<EventTestController> _logger;

    public EventTestController(IEventBus eventBus, ILogger<EventTestController> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Test publishing User Lifecycle Events
    /// </summary>
    [HttpPost("user-events")]
    public async Task<IActionResult> TestUserEvents()
    {
        var testUserId = Guid.NewGuid();
        var testEmail = $"test.user.{DateTime.Now:MMddHHmmss}@example.com";

        try
        {
            // 1. Test UserCreatedEvent
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                FullName = "Test User Full Name",
                PhoneNumber = "+1234567890",
                Roles = new List<string> { "User", "ContentCreator" },
                Status = EntityStatusEnum.Active,
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false,
                ProfileData = new Dictionary<string, object>
                {
                    ["preferences"] = "dark_mode",
                    ["language"] = "vi-VN"
                }
            };

            await _eventBus.PublishAsync(userCreatedEvent);
            _logger.LogInformation("Published UserCreatedEvent for UserId: {UserId}", testUserId);

            // Wait a bit for processing
            await Task.Delay(1000);

            // 2. Test UserUpdatedEvent
            var userUpdatedEvent = new UserUpdatedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                FullName = "Updated Full Name",
                PhoneNumber = "+0987654321",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = testUserId,
                ChangedFields = new List<string> { "FullName", "PhoneNumber" },
                ProfileData = new Dictionary<string, object>
                {
                    ["preferences"] = "light_mode",
                    ["language"] = "en-US"
                }
            };

            await _eventBus.PublishAsync(userUpdatedEvent);
            _logger.LogInformation("Published UserUpdatedEvent for UserId: {UserId}", testUserId);

            await Task.Delay(1000);

            // 3. Test UserEmailVerifiedEvent
            var emailVerifiedEvent = new UserEmailVerifiedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                VerifiedAt = DateTime.UtcNow,
                VerificationMethod = "Email"
            };

            await _eventBus.PublishAsync(emailVerifiedEvent);
            _logger.LogInformation("Published UserEmailVerifiedEvent for UserId: {UserId}", testUserId);

            await Task.Delay(1000);

            // 4. Test UserActivationChangedEvent
            var activationChangedEvent = new UserActivationChangedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                IsActive = false,
                OldStatus = EntityStatusEnum.Active,
                NewStatus = EntityStatusEnum.Inactive,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = Guid.NewGuid(), // Admin user
                Reason = "Test deactivation"
            };

            await _eventBus.PublishAsync(activationChangedEvent);
            _logger.LogInformation("Published UserActivationChangedEvent for UserId: {UserId}", testUserId);

            return Ok(new
            {
                success = true,
                message = "Successfully published all User Lifecycle Events!",
                testUserId = testUserId,
                testEmail = testEmail,
                eventsPublished = new[]
                {
                    nameof(UserCreatedEvent),
                    nameof(UserUpdatedEvent), 
                    nameof(UserEmailVerifiedEvent),
                    nameof(UserActivationChangedEvent)
                },
                publishedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing User Events for UserId: {UserId}", testUserId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish User Events",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test publishing Authentication Events
    /// </summary>
    [HttpPost("auth-events")]
    public async Task<IActionResult> TestAuthEvents()
    {
        var testUserId = Guid.NewGuid();
        var testEmail = $"auth.test.{DateTime.Now:MMddHHmmss}@example.com";

        try
        {
            // 1. Test UserLoggedInEvent
            var loginEvent = new UserLoggedInEvent
            {
                UserId = testUserId,
                Email = testEmail,
                Roles = new List<string> { "User", "Moderator" },
                RefreshToken = "test_refresh_token_" + Guid.NewGuid(),
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30),
                LoginAt = DateTime.UtcNow,
                UserAgent = "Test User Agent",
                IpAddress = "192.168.1.100"
            };

            await _eventBus.PublishAsync(loginEvent);
            _logger.LogInformation("Published UserLoggedInEvent for UserId: {UserId}", testUserId);

            await Task.Delay(1000);

            // 2. Test UserStatusChangedEvent
            var statusChangedEvent = new UserStatusChangedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                OldStatus = EntityStatusEnum.Active,
                NewStatus = EntityStatusEnum.Inactive,
                ChangedBy = Guid.NewGuid(), // Admin user
                ChangedAt = DateTime.UtcNow,
                Reason = "Suspicious activity detected - test event"
            };

            await _eventBus.PublishAsync(statusChangedEvent);
            _logger.LogInformation("Published UserStatusChangedEvent for UserId: {UserId}", testUserId);

            await Task.Delay(1000);

            // 3. Test RoleAddedToUserEvent
            var roleAddedEvent = new RoleAddedToUserEvent
            {
                UserId = testUserId,
                Email = testEmail,
                RoleName = "PremiumUser",
                AddedBy = Guid.NewGuid(), // Admin user
                AddedAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(roleAddedEvent);
            _logger.LogInformation("Published RoleAddedToUserEvent for UserId: {UserId}", testUserId);

            await Task.Delay(1000);

            // 4. Test UserLoggedOutEvent
            var logoutEvent = new UserLoggedOutEvent
            {
                UserId = testUserId,
                Email = testEmail,
                LogoutAt = DateTime.UtcNow,
                LogoutType = "Manual"
            };

            await _eventBus.PublishAsync(logoutEvent);
            _logger.LogInformation("Published UserLoggedOutEvent for UserId: {UserId}", testUserId);

            return Ok(new
            {
                success = true,
                message = "Successfully published all Authentication Events!",
                testUserId = testUserId,
                testEmail = testEmail,
                eventsPublished = new[]
                {
                    nameof(UserLoggedInEvent),
                    nameof(UserStatusChangedEvent),
                    nameof(RoleAddedToUserEvent),
                    nameof(UserLoggedOutEvent)
                },
                publishedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing Auth Events for UserId: {UserId}", testUserId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish Auth Events",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test publishing Content Events
    /// </summary>
    [HttpPost("content-events")]
    public async Task<IActionResult> TestContentEvents()
    {
        var testUserId = Guid.NewGuid();
        var testContentId = Guid.NewGuid();

        try
        {
            // 1. Test PodcastCreatedEvent
            var podcastCreatedEvent = new PodcastCreatedEvent(
                testContentId,
                "Test Podcast Episode: Complete Event Flow",
                "This is a comprehensive test podcast to verify the complete event-driven architecture integration with RabbitMQ and MassTransit consumers.",
                "https://example.com/test-podcast-audio.mp3",
                TimeSpan.FromMinutes(25),
                testUserId,
                DateTime.UtcNow,
                new[] { "test", "rabbitmq", "masstransit", "event-driven" },
                new[] { EmotionCategory.Happiness, EmotionCategory.Hope },
                new[] { TopicCategory.MentalHealth, TopicCategory.Mindfulness }
            );

            await _eventBus.PublishAsync(podcastCreatedEvent);
            _logger.LogInformation("Published PodcastCreatedEvent for ContentId: {ContentId}", testContentId);

            await Task.Delay(1000);

            // 2. Test PodcastPublishedEvent  
            var podcastPublishedEvent = new PodcastPublishedEvent(
                testContentId,
                "Test Podcast Episode: Complete Event Flow",
                "This is a comprehensive test podcast to verify the complete event-driven architecture integration.",
                "https://example.com/test-podcast-audio.mp3",
                TimeSpan.FromMinutes(25),
                testUserId,
                Guid.NewGuid(), // Moderator who approved
                DateTime.UtcNow,
                new[] { "test", "rabbitmq", "masstransit", "event-driven" },
                new[] { EmotionCategory.Happiness, EmotionCategory.Hope },
                new[] { TopicCategory.MentalHealth, TopicCategory.Mindfulness }
            );

            await _eventBus.PublishAsync(podcastPublishedEvent);
            _logger.LogInformation("Published PodcastPublishedEvent for ContentId: {ContentId}", testContentId);

            await Task.Delay(1000);

            // 3. Test CommunityStoryCreatedEvent
            var communityStoryId = Guid.NewGuid();
            var storyCreatedEvent = new CommunityStoryCreatedEvent(
                communityStoryId,
                "My Journey with Mental Health - A Test Story",
                "This is a test community story to verify event processing...",
                testUserId,
                DateTime.UtcNow,
                new[] { EmotionCategory.Hope, EmotionCategory.Gratitude },
                new[] { "mental-health", "recovery", "test" }
            );

            await _eventBus.PublishAsync(storyCreatedEvent);
            _logger.LogInformation("Published CommunityStoryCreatedEvent for ContentId: {ContentId}", communityStoryId);

            return Ok(new
            {
                success = true,
                message = "Successfully published all Content Events!",
                testUserId = testUserId,
                testContentIds = new
                {
                    podcastId = testContentId,
                    storyId = communityStoryId
                },
                eventsPublished = new[]
                {
                    nameof(PodcastCreatedEvent),
                    nameof(PodcastPublishedEvent),
                    nameof(CommunityStoryCreatedEvent)
                },
                publishedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing Content Events");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to publish Content Events",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Test complete workflow: User → Content → Events
    /// </summary>
    [HttpPost("complete-workflow")]
    public async Task<IActionResult> TestCompleteWorkflow()
    {
        var testUserId = Guid.NewGuid();
        var testEmail = $"workflow.test.{DateTime.Now:MMddHHmmss}@example.com";
        var testContentId = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Starting complete workflow test for UserId: {UserId}", testUserId);

            // Step 1: User Creation
            await _eventBus.PublishAsync(new UserCreatedEvent
            {
                UserId = testUserId,
                Email = testEmail,
                FullName = "Workflow Test User",
                Roles = new List<string> { "User" },
                Status = EntityStatusEnum.Active,
                CreatedAt = DateTime.UtcNow
            });

            await Task.Delay(2000); // Wait for processing

            // Step 2: User Login
            await _eventBus.PublishAsync(new UserLoggedInEvent
            {
                UserId = testUserId,
                Email = testEmail,
                Roles = new List<string> { "User" },
                LoginAt = DateTime.UtcNow,
                UserAgent = "Workflow Test Agent",
                IpAddress = "127.0.0.1"
            });

            await Task.Delay(2000);

            // Step 3: Content Creation
            await _eventBus.PublishAsync(new PodcastCreatedEvent(
                testContentId,
                "Workflow Test Podcast",
                "Testing complete user → content → event flow",
                "https://example.com/workflow-test.mp3",
                TimeSpan.FromMinutes(10),
                testUserId,
                DateTime.UtcNow,
                new[] { "workflow", "test" },
                new[] { EmotionCategory.Happiness },
                new[] { TopicCategory.MentalHealth }
            ));

            await Task.Delay(2000);

            // Step 4: Content Approved
            await _eventBus.PublishAsync(new PodcastPublishedEvent(
                testContentId,
                "Workflow Test Podcast",
                "Testing complete user → content → event flow",
                "https://example.com/workflow-test.mp3",
                TimeSpan.FromMinutes(10),
                testUserId,
                Guid.NewGuid(), // Moderator
                DateTime.UtcNow,
                new[] { "workflow", "test" },
                new[] { EmotionCategory.Happiness },
                new[] { TopicCategory.MentalHealth }
            ));

            await Task.Delay(2000);

            // Step 5: User Role Change
            await _eventBus.PublishAsync(new RoleAddedToUserEvent
            {
                UserId = testUserId,
                Email = testEmail,
                RoleName = "ContentCreator",
                AddedBy = Guid.NewGuid(),
                AddedAt = DateTime.UtcNow
            });

            await Task.Delay(2000);

            // Step 6: User Logout
            await _eventBus.PublishAsync(new UserLoggedOutEvent
            {
                UserId = testUserId,
                Email = testEmail,
                LogoutAt = DateTime.UtcNow,
                LogoutType = "Manual"
            });

            _logger.LogInformation("Completed workflow test for UserId: {UserId}", testUserId);

            return Ok(new
            {
                success = true,
                message = "Successfully completed entire workflow test!",
                workflow = new
                {
                    userId = testUserId,
                    email = testEmail,
                    contentId = testContentId,
                    steps = new[]
                    {
                        "1. User Created",
                        "2. User Logged In", 
                        "3. Content Created",
                        "4. Content Approved",
                        "5. Role Added",
                        "6. User Logged Out"
                    }
                },
                completedAt = DateTime.UtcNow,
                note = "Check ContentService logs and RabbitMQ UI to verify event processing!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complete workflow test for UserId: {UserId}", testUserId);
            return StatusCode(500, new
            {
                success = false,
                message = "Complete workflow test failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get event testing status and recommendations
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetTestingStatus()
    {
        return Ok(new
        {
            service = "ContentService Event Testing",
            status = "Ready",
            availableTests = new[]
            {
                "POST /api/eventtest/user-events - Test User Lifecycle Events",
                "POST /api/eventtest/auth-events - Test Authentication Events", 
                "POST /api/eventtest/content-events - Test Content Events",
                "POST /api/eventtest/complete-workflow - Test Complete User→Content Flow"
            },
            rabbitMqUI = "http://localhost:15672 (guest/guest)",
            recommendations = new[]
            {
                "1. Check RabbitMQ UI for queue activity",
                "2. Monitor ContentService logs for event processing",
                "3. Verify consumer activity in application logs",
                "4. Test individual event types before complete workflow"
            },
            timestamp = DateTime.UtcNow
        });
    }
}
