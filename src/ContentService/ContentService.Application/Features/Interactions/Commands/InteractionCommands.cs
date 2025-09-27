using MediatR;

namespace ContentService.Application.Features.Interactions.Commands;

/// <summary>
/// Command để like/unlike content
/// </summary>
public record LikeContentCommand(
    Guid ContentId,
    bool IsLiked
) : IRequest<bool>;

/// <summary>
/// Command để track content view
/// </summary>
public record ViewContentCommand(
    Guid ContentId,
    string? UserAgent = null,
    string? IpAddress = null
) : IRequest<bool>;

/// <summary>
/// Command để share content
/// </summary>
public record ShareContentCommand(
    Guid ContentId,
    string ShareMethod // email, facebook, twitter, link, etc.
) : IRequest<bool>;

/// <summary>
/// Command để rate content
/// </summary>
public record RateContentCommand(
    Guid ContentId,
    int Rating, // 1-5 stars
    string? Review = null
) : IRequest<bool>;

/// <summary>
/// Command để comment on content
/// </summary>
public record CommentOnContentCommand(
    Guid ContentId,
    string CommentText
) : IRequest<Guid>; // Returns comment ID

/// <summary>
/// Command để delete comment
/// </summary>
public record DeleteCommentCommand(
    Guid CommentId
) : IRequest<bool>;
