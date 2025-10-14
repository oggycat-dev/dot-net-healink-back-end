using MediatR;
using Microsoft.EntityFrameworkCore;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Outbox;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class IncrementPodcastViewHandler : IRequestHandler<IncrementPodcastViewCommand>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;

    public IncrementPodcastViewHandler(IOutboxUnitOfWork outboxUnitOfWork)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
    }

    public async Task Handle(IncrementPodcastViewCommand request, CancellationToken cancellationToken)
    {
        var podcast = await _outboxUnitOfWork.Repository<Content>()
            .GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ContentType == ContentType.Podcast, cancellationToken);

        if (podcast == null)
        {
            throw new KeyNotFoundException($"Podcast with ID {request.Id} not found");
        }

        // Increment view count
        podcast.ViewCount++;

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
        await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
    }
}

public class TogglePodcastLikeHandler : IRequestHandler<TogglePodcastLikeCommand, int>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;

    public TogglePodcastLikeHandler(IOutboxUnitOfWork outboxUnitOfWork)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
    }

    public async Task<int> Handle(TogglePodcastLikeCommand request, CancellationToken cancellationToken)
    {
        var podcast = await _outboxUnitOfWork.Repository<Content>()
            .GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ContentType == ContentType.Podcast, cancellationToken);

        if (podcast == null)
        {
            throw new KeyNotFoundException($"Podcast with ID {request.Id} not found");
        }

        // Check if user already liked this podcast
        var existingLike = await _outboxUnitOfWork.Repository<ContentInteraction>()
            .GetQueryable()
            .FirstOrDefaultAsync(
                i => i.ContentId == request.Id 
                     && i.UserId == request.UserId 
                     && i.InteractionType == InteractionType.Like,
                cancellationToken);

        if (existingLike != null)
        {
            // Unlike: remove the interaction and decrement count
            _outboxUnitOfWork.Repository<ContentInteraction>().Delete(existingLike);
            podcast.LikeCount = Math.Max(0, podcast.LikeCount - 1); // Ensure non-negative
        }
        else
        {
            // Like: add new interaction and increment count
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

        return podcast.LikeCount;
    }
}
