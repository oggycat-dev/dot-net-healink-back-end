using MediatR;
using ContentService.Domain.Enums;

namespace ContentService.Application.Features.Podcasts.Queries;

public record GetPodcastsQuery(
    int Page = 1,
    int PageSize = 10,
    ContentStatus? Status = null,
    EmotionCategory[]? EmotionCategories = null,
    TopicCategory[]? TopicCategories = null,
    string? SearchTerm = null,
    string? SeriesName = null
) : IRequest<GetPodcastsResponse>;

public record GetPodcastsResponse(
    IEnumerable<PodcastDto> Podcasts,
    int TotalCount,
    int Page,
    int PageSize
);

public record PodcastDto(
    Guid Id,
    string Title,
    string Description,
    string? ThumbnailUrl,
    string AudioUrl,
    TimeSpan Duration,
    string? TranscriptUrl,
    string? HostName,
    string? GuestName,
    int EpisodeNumber,
    string? SeriesName,
    string[] Tags,
    EmotionCategory[] EmotionCategories,
    TopicCategory[] TopicCategories,
    ContentStatus ContentStatus,
    int ViewCount,
    int LikeCount,
    DateTime? CreatedAt,
    DateTime? PublishedAt,
    Guid? CreatedBy
);