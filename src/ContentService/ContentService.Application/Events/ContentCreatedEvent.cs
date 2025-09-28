using SharedLibrary.Commons.EventBus;
using ContentService.Domain.Enums;

namespace ContentService.Application.Events;

/// <summary>
/// Event khi content được tạo mới (Draft state)
/// </summary>
public record ContentCreatedEvent(
    Guid ContentId,
    string Title,
    string Description,
    ContentType ContentType,
    ContentStatus Status,
    Guid CreatedBy,
    DateTime CreatedAt,
    string[]? Tags = null
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi content được cập nhật
/// </summary>
public record ContentUpdatedEvent(
    Guid ContentId,
    string Title,
    string Description,
    ContentType ContentType,
    ContentStatus Status,
    Guid UpdatedBy,
    DateTime UpdatedAt,
    string[]? Tags = null
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi content được duyệt và publish
/// </summary>
public record ContentPublishedEvent(
    Guid ContentId,
    string Title,
    ContentType ContentType,
    Guid CreatedBy,
    Guid ApprovedBy,
    DateTime PublishedAt,
    string[]? Tags = null
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi content bị từ chối
/// </summary>
public record ContentRejectedEvent(
    Guid ContentId,
    string Title,
    ContentType ContentType,
    Guid CreatedBy,
    Guid RejectedBy,
    string RejectionReason,
    DateTime RejectedAt
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi content bị xóa
/// </summary>
public record ContentDeletedEvent(
    Guid ContentId,
    string Title,
    ContentType ContentType,
    Guid CreatedBy,
    Guid? DeletedBy,
    DateTime DeletedAt
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi content được approve (chuyển từ Draft/Review sang Approved)
/// </summary>
public record ContentApprovedEvent(
    Guid ContentId,
    string Title,
    ContentType ContentType,
    Guid CreatedBy,
    Guid ApprovedBy,
    DateTime ApprovedAt,
    string? ApprovalNotes = null
) : IntegrationEvent("ContentService");

/// <summary>
/// Event khi có view mới cho content
/// </summary>
public record ContentViewedEvent(
    Guid ContentId,
    ContentType ContentType,
    Guid? ViewedBy, // null nếu là anonymous view
    DateTime ViewedAt,
    string? UserAgent = null,
    string? IpAddress = null
) : IntegrationEvent("ContentService");