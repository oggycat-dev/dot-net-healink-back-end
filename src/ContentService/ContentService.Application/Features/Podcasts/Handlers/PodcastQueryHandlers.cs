using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Domain.Entities;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Services;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class GetPodcastsQueryHandler : IRequestHandler<GetPodcastsQuery, GetPodcastsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserStateCache _userStateCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPodcastsQueryHandler> _logger;

    public GetPodcastsQueryHandler(
        IUnitOfWork unitOfWork,
        IUserStateCache userStateCache,
        ICurrentUserService currentUserService,
        ILogger<GetPodcastsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userStateCache = userStateCache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetPodcastsResponse> Handle(GetPodcastsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Skip subscription check for internal requests (e.g. AI Recommendation Service)
            if (!request.IsInternalRequest)
            {
                await ValidateSubscriptionAccessAsync();
            }
            
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

    /// <summary>
    /// Validate subscription access from cache
    /// ✅ Admin, Staff, ContentCreator: No subscription required
    /// ❌ User: Must have active subscription
    /// </summary>
    private async Task ValidateSubscriptionAccessAsync()
    {
        var userId = _currentUserService.UserId;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt - No valid user ID");
            throw new UnauthorizedAccessException("Authentication required to view podcasts");
        }

        // Get user state from cache
        var userState = await _userStateCache.GetUserStateAsync(userGuid);
        
        if (userState == null || !userState.IsActive)
        {
            _logger.LogWarning("Access denied - User {UserId} is not active or not found in cache", userGuid);
            throw new UnauthorizedAccessException("User account is not active");
        }

        // ✅ Check roles - Admin, Staff, ContentCreator bypass subscription check
        var exemptRoles = new[] { "Admin", "Staff", "ContentCreator" };
        var hasExemptRole = userState.Roles.Any(role => exemptRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        
        if (hasExemptRole)
        {
            _logger.LogInformation("User {UserId} has exempt role ({Roles}) - Subscription check bypassed", 
                userGuid, string.Join(", ", userState.Roles));
            return;
        }

        // ❌ Regular User - Must have active subscription
        if (!userState.HasActiveSubscription)
        {
            _logger.LogWarning("Access denied - User {UserId} does not have active subscription", userGuid);
            throw new UnauthorizedAccessException("Active subscription required to view podcasts. Please subscribe to continue.");
        }

        _logger.LogInformation("User {UserId} has active subscription - Access granted", userGuid);
    }
}