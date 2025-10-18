using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Repositories;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class CreatorDashboardStatsHandler : IRequestHandler<GetCreatorDashboardStatsQuery, CreatorDashboardStatsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatorDashboardStatsHandler> _logger;

    public CreatorDashboardStatsHandler(IUnitOfWork unitOfWork, ILogger<CreatorDashboardStatsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreatorDashboardStatsResponse> Handle(GetCreatorDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Podcast>().GetQueryable().Where(p => p.CreatedBy == request.CreatorId && !p.IsDeleted);

        var totalPodcasts = await query.CountAsync(cancellationToken);
        var publishedPodcasts = await query.Where(p => p.ContentStatus == ContentStatus.Published).CountAsync(cancellationToken);
        var pendingPodcasts = await query.Where(p => p.ContentStatus == ContentStatus.PendingReview || p.ContentStatus == ContentStatus.PendingModeration).CountAsync(cancellationToken);
        var rejectedPodcasts = await query.Where(p => p.ContentStatus == ContentStatus.Rejected).CountAsync(cancellationToken);

        var totals = await query.Select(p => new { p.ViewCount, p.LikeCount }).ToListAsync(cancellationToken);
        var totalViews = totals.Sum(t => t.ViewCount);
        var totalLikes = totals.Sum(t => t.LikeCount);

        var topPodcasts = await query
            .OrderByDescending(p => p.ViewCount)
            .ThenByDescending(p => p.LikeCount)
            .Take(10)
            .Select(p => new CreatorPodcastStatItem(
                p.Id,
                p.Title,
                p.ViewCount,
                p.LikeCount,
                p.PublishedAt
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Computed dashboard stats for creator {CreatorId}: total={Total}, views={Views}, likes={Likes}", request.CreatorId, totalPodcasts, totalViews, totalLikes);

        return new CreatorDashboardStatsResponse(
            totalPodcasts,
            publishedPodcasts,
            pendingPodcasts,
            rejectedPodcasts,
            totalViews,
            totalLikes,
            topPodcasts
        );
    }
}


