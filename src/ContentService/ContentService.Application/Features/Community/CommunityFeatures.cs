using MediatR;
using ContentService.Domain.Enums;

namespace ContentService.Application.Features.Community.Commands;

public record CreateCommunityStoryCommand(
    string Title,
    string Description,
    string StoryContent,
    bool IsAnonymous,
    string? AuthorDisplayName,
    string[] Tags,
    EmotionCategory[] EmotionCategories,
    TopicCategory[] TopicCategories,
    string[] TriggerWarnings,
    Guid CreatedBy
) : IRequest<CreateCommunityStoryResponse>;

public record CreateCommunityStoryResponse(
    Guid Id,
    string Title,
    ContentStatus ContentStatus,
    DateTime? CreatedAt
);

public record ApproveCommunityStoryCommand(
    Guid Id,
    Guid ApprovedBy,
    bool IsModeratorPick = false
) : IRequest<ApproveCommunityStoryResponse>;

public record ApproveCommunityStoryResponse(
    Guid Id,
    ContentStatus ContentStatus,
    bool IsModeratorPick,
    DateTime? ApprovedAt
);

public record RejectCommunityStoryCommand(
    Guid Id,
    Guid RejectedBy,
    string Reason
) : IRequest<bool>;