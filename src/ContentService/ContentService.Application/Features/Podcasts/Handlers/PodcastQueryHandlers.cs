using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Domain.Entities;
using SharedLibrary.Commons.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class GetPodcastsQueryHandler : IRequestHandler<GetPodcastsQuery, GetPodcastsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPodcastsQueryHandler> _logger;

    public GetPodcastsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPodcastsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetPodcastsResponse> Handle(GetPodcastsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Repository<Podcast>().GetQueryable();

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(p => p.ContentStatus == request.Status.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(p => p.Title.Contains(request.SearchTerm) || 
                                       p.Description.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrEmpty(request.SeriesName))
            {
                query = query.Where(p => p.SeriesName == request.SeriesName);
            }

            if (request.EmotionCategories?.Length > 0)
            {
                // Filter by emotion categories (this would need custom implementation based on your JSON storage)
                // For now, simplified approach
                foreach (var emotion in request.EmotionCategories)
                {
                    query = query.Where(p => p.EmotionCategories.Contains(emotion));
                }
            }

            if (request.TopicCategories?.Length > 0)
            {
                // Filter by topic categories
                foreach (var topic in request.TopicCategories)
                {
                    query = query.Where(p => p.TopicCategories.Contains(topic));
                }
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var podcasts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PodcastDto(
                    p.Id,
                    p.Title,
                    p.Description,
                    p.ThumbnailUrl,
                    p.AudioUrl,
                    p.Duration,
                    p.TranscriptUrl,
                    p.HostName,
                    p.GuestName,
                    p.EpisodeNumber,
                    p.SeriesName,
                    p.Tags,
                    p.EmotionCategories,
                    p.TopicCategories,
                    p.ContentStatus,
                    p.ViewCount,
                    p.LikeCount,
                    p.CreatedAt,
                    p.PublishedAt,
                    p.CreatedBy
                ))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} podcasts out of {Total} total podcasts", podcasts.Count, totalCount);

            return new GetPodcastsResponse(
                podcasts,
                totalCount,
                request.Page,
                request.PageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving podcasts");
            throw;
        }
    }
}