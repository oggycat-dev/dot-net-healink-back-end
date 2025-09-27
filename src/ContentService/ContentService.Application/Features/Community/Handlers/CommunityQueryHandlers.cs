using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Community.Queries;
using ContentService.Domain.Entities;
using SharedLibrary.Commons.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Application.Features.Community.Handlers;

public class GetCommunityStoriesQueryHandler : IRequestHandler<GetCommunityStoriesQuery, GetCommunityStoriesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCommunityStoriesQueryHandler> _logger;

    public GetCommunityStoriesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetCommunityStoriesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetCommunityStoriesResponse> Handle(GetCommunityStoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Repository<CommunityStory>().GetQueryable();

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(s => s.ContentStatus == request.Status.Value);
            }

            if (request.IsModeratorPick.HasValue)
            {
                query = query.Where(s => s.IsModeratorPick == request.IsModeratorPick.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(s => s.Title.Contains(request.SearchTerm) || 
                                       s.Description.Contains(request.SearchTerm) ||
                                       s.StoryContent.Contains(request.SearchTerm));
            }

            if (request.EmotionCategories?.Length > 0)
            {
                // Filter by emotion categories
                foreach (var emotion in request.EmotionCategories)
                {
                    query = query.Where(s => s.EmotionCategories.Contains(emotion));
                }
            }

            if (request.TopicCategories?.Length > 0)
            {
                // Filter by topic categories
                foreach (var topic in request.TopicCategories)
                {
                    query = query.Where(s => s.TopicCategories.Contains(topic));
                }
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering
            var stories = await query
                .OrderByDescending(s => s.IsModeratorPick)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new CommunityStoryDto(
                    s.Id,
                    s.Title,
                    s.Description,
                    s.StoryContent,
                    s.IsAnonymous,
                    s.AuthorDisplayName,
                    s.ContentStatus,
                    s.Tags,
                    s.EmotionCategories,
                    s.TopicCategories,
                    s.TriggerWarnings,
                    s.ViewCount,
                    s.LikeCount,
                    s.HelpfulCount,
                    s.CommentCount,
                    s.IsModeratorPick,
                    s.CreatedAt,
                    s.PublishedAt,
                    s.CreatedBy,
                    s.ApprovedBy
                ))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} community stories out of {Total} total stories", stories.Count, totalCount);

            return new GetCommunityStoriesResponse(
                stories,
                totalCount,
                request.Page,
                request.PageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving community stories");
            throw;
        }
    }
}