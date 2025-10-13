using SharedLibrary.Commons.EventBus;
using ContentService.Domain.Enums;

namespace ContentService.Application.Events;

/// <summary>
/// Podcast-specific events cho recommendation service và analytics
/// </summary>

public record PodcastCreatedEvent(
    Guid PodcastId,
    string Title,
    string Description,
    string? AudioUrl,
    TimeSpan? Duration,
    Guid? CreatedBy,
    DateTime CreatedAt,
    string[]? Tags = null,
    EmotionCategory[]? EmotionCategories = null,
    TopicCategory[]? TopicCategories = null
) : IntegrationEvent("ContentService");

public record PodcastPublishedEvent(
    Guid PodcastId,
    string Title,
    string Description,
    string AudioUrl,
    TimeSpan Duration,
    Guid? CreatedBy,
    Guid ApprovedBy,
    DateTime PublishedAt,
    string[]? Tags = null,
    EmotionCategory[]? EmotionCategories = null,
    TopicCategory[]? TopicCategories = null
) : IntegrationEvent("ContentService");

public record PodcastPlayedEvent(
    Guid PodcastId,
    Guid? UserId, // null cho anonymous users
    DateTime PlayedAt,
    TimeSpan? Position = null, // vị trí bắt đầu play
    TimeSpan? Duration = null, // thời gian nghe
    bool IsCompleted = false
) : IntegrationEvent("ContentService");

public record PodcastLikedEvent(
    Guid PodcastId,
    Guid UserId,
    DateTime LikedAt,
    bool IsLiked // true = like, false = unlike
) : IntegrationEvent("ContentService");

public record PodcastSharedEvent(
    Guid PodcastId,
    Guid? SharedBy,
    string ShareMethod, // email, social, link, etc.
    DateTime SharedAt
) : IntegrationEvent("ContentService");

public record PodcastRatedEvent(
    Guid PodcastId,
    Guid UserId,
    int Rating, // 1-5 stars
    string? Review,
    DateTime RatedAt
) : IntegrationEvent("ContentService");
