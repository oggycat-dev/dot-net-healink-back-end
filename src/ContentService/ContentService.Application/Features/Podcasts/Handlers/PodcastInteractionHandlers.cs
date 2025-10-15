using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Outbox;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class IncrementPodcastViewHandler : IRequestHandler<IncrementPodcastViewCommand>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<IncrementPodcastViewHandler> _logger;

    public IncrementPodcastViewHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<IncrementPodcastViewHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
    }

    public async Task Handle(IncrementPodcastViewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to increment view for podcast: {PodcastId}", request.Id);
        
        var podcast = await _outboxUnitOfWork.Repository<Content>()
            .GetQueryable()
            .AsTracking() // Ensure entity is tracked
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ContentType == ContentType.Podcast, cancellationToken);

        if (podcast == null)
        {
            _logger.LogWarning("Podcast not found: {PodcastId}", request.Id);
            throw new KeyNotFoundException($"Podcast with ID {request.Id} not found");
        }

        _logger.LogInformation("Found podcast: {PodcastId}, Current ViewCount: {ViewCount}", request.Id, podcast.ViewCount);

        // Increment view count
        podcast.ViewCount++;
        
        _logger.LogInformation("Incremented ViewCount to: {ViewCount}", podcast.ViewCount);

        // Optionally record the view interaction (anonymous views)
        var viewInteraction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            ContentId = request.Id,
            UserId = Guid.Empty, // Anonymous view
            InteractionType = InteractionType.View,
            InteractionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _outboxUnitOfWork.Repository<ContentInteraction>().AddAsync(viewInteraction);
        
        _logger.LogInformation("Saving changes to database...");
        await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
        
        _logger.LogInformation("Successfully saved changes. View count for podcast: {PodcastId}, New count: {ViewCount}", 
            request.Id, podcast.ViewCount);
    }
}

public class TogglePodcastLikeHandler : IRequestHandler<TogglePodcastLikeCommand, int>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<TogglePodcastLikeHandler> _logger;

    public TogglePodcastLikeHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<TogglePodcastLikeHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(TogglePodcastLikeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling like for podcast: {PodcastId} by user: {UserId}", request.Id, request.UserId);
        
        var podcast = await _outboxUnitOfWork.Repository<Content>()
            .GetQueryable()
            .AsTracking() // Ensure entity is tracked
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ContentType == ContentType.Podcast, cancellationToken);

        if (podcast == null)
        {
            _logger.LogWarning("Podcast not found: {PodcastId}", request.Id);
            throw new KeyNotFoundException($"Podcast with ID {request.Id} not found");
        }

        // Check if user already liked this podcast
        var existingLike = await _outboxUnitOfWork.Repository<ContentInteraction>()
            .GetQueryable()
            .AsTracking() // Ensure entity is tracked for deletion
            .FirstOrDefaultAsync(
                i => i.ContentId == request.Id 
                     && i.UserId == request.UserId 
                     && i.InteractionType == InteractionType.Like,
                cancellationToken);

        if (existingLike != null)
        {
            // Unlike: remove the interaction and decrement count
            _logger.LogInformation("Removing like for podcast: {PodcastId}, Current count: {LikeCount}", request.Id, podcast.LikeCount);
            _outboxUnitOfWork.Repository<ContentInteraction>().Delete(existingLike);
            podcast.LikeCount = Math.Max(0, podcast.LikeCount - 1); // Ensure non-negative
        }
        else
        {
            // Like: add new interaction and increment count
            _logger.LogInformation("Adding like for podcast: {PodcastId}, Current count: {LikeCount}", request.Id, podcast.LikeCount);
            var likeInteraction = new ContentInteraction
            {
                Id = Guid.NewGuid(),
                ContentId = request.Id,
                UserId = request.UserId,
                InteractionType = InteractionType.Like,
                InteractionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _outboxUnitOfWork.Repository<ContentInteraction>().AddAsync(likeInteraction);
            podcast.LikeCount++;
        }

        await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
        
        _logger.LogInformation("Successfully toggled like for podcast: {PodcastId}, New count: {LikeCount}", 
            request.Id, podcast.LikeCount);

        return podcast.LikeCount;
    }
}
