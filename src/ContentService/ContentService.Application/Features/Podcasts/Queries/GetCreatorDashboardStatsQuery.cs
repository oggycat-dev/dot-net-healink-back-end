using MediatR;

namespace ContentService.Application.Features.Podcasts.Queries;

public record GetCreatorDashboardStatsQuery(Guid CreatorId) : IRequest<CreatorDashboardStatsResponse>;

public record CreatorDashboardStatsResponse(
    int TotalPodcasts,
    int PublishedPodcasts,
    int PendingPodcasts,
    int RejectedPodcasts,
    int TotalViews,
    int TotalLikes,
    IEnumerable<CreatorPodcastStatItem> TopPodcasts
);

public record CreatorPodcastStatItem(
    Guid Id,
    string Title,
    int ViewCount,
    int LikeCount,
    DateTime? PublishedAt
);


