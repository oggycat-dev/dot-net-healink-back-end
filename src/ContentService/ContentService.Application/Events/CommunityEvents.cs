using SharedLibrary.Commons.EventBus;
using ContentService.Domain.Enums;

namespace ContentService.Application.Events;

/// <summary>
/// Community-specific events để track user interactions và content moderation
/// </summary>

public record CommunityStoryCreatedEvent(
    Guid StoryId,
    string Title,
    string StoryContent,
    Guid CreatedBy,
    DateTime CreatedAt,
    EmotionCategory[]? EmotionCategories = null,
    string[]? Tags = null
) : IntegrationEvent("ContentService");

public record CommunityStoryApprovedEvent(
    Guid StoryId,
    string Title,
    Guid CreatedBy,
    Guid ApprovedBy,
    DateTime ApprovedAt,
    string? ModerationNotes = null
) : IntegrationEvent("ContentService");

public record CommunityStoryRejectedEvent(
    Guid StoryId,
    string Title,
    Guid CreatedBy,
    Guid RejectedBy,
    DateTime RejectedAt,
    string RejectionReason
) : IntegrationEvent("ContentService");

public record CommunityStoryMarkedHelpfulEvent(
    Guid StoryId,
    Guid MarkedBy,
    DateTime MarkedAt,
    bool IsHelpful // true = helpful, false = remove helpful mark
) : IntegrationEvent("ContentService");

public record CommunityStorySharedEvent(
    Guid StoryId,
    Guid? SharedBy,
    string ShareMethod,
    DateTime SharedAt
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi có user interaction với community content
/// </summary>
public record CommunityEngagementEvent(
    Guid ContentId,
    Guid UserId,
    string EngagementType, // view, like, comment, share
    DateTime EngagementAt,
    string? AdditionalData = null // JSON data for specific engagement details
) : IntegrationEvent("ContentService");
