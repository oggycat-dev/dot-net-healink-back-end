using MediatR;
using ContentService.Domain.Enums;

namespace ContentService.Application.Features.Community.Queries;

public record GetCommunityStoriesQuery(
    int Page = 1,
    int PageSize = 10,
    ContentStatus? Status = null,
    bool? IsModeratorPick = null,
    EmotionCategory[]? EmotionCategories = null,
    TopicCategory[]? TopicCategories = null,
    string? SearchTerm = null
) : IRequest<GetCommunityStoriesResponse>;

public record GetCommunityStoriesResponse(
    IEnumerable<CommunityStoryDto> Stories,
    int TotalCount,
    int Page,
    int PageSize
);

public record CommunityStoryDto(
    Guid Id,
    string Title,
    string Description,
    string StoryContent,
    bool IsAnonymous,
    string? AuthorDisplayName,
    ContentStatus ContentStatus,
    string[] Tags,
    EmotionCategory[] EmotionCategories,
    TopicCategory[] TopicCategories,
    string[] TriggerWarnings,
    int ViewCount,
    int LikeCount,
    int HelpfulCount,
    int CommentCount,
    bool IsModeratorPick,
    DateTime? CreatedAt,
    DateTime? PublishedAt,
    Guid? CreatedBy,
    Guid? ApprovedBy
);