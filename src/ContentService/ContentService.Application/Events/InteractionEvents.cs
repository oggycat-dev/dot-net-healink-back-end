using SharedLibrary.Commons.EventBus;
using ContentService.Domain.Enums;

namespace ContentService.Application.Events;

public record CommentCreatedEvent(
    Guid CommentId,
    Guid ContentId,
    Guid UserId,
    string CommentText,
    DateTime CreatedAt
) : IntegrationEvent("ContentService");

public record CommentDeletedEvent(
    Guid CommentId,
    Guid ContentId,
    Guid UserId,
    DateTime DeletedAt
) : IntegrationEvent("ContentService");

public record ContentInteractionEvent(
    Guid ContentId,
    Guid UserId,
    InteractionType InteractionType,
    DateTime InteractionAt
) : IntegrationEvent("ContentService");

public record ContentRatedEvent(
    Guid ContentId,
    Guid UserId,
    int Rating,
    string? Review,
    DateTime RatedAt
) : IntegrationEvent("ContentService");