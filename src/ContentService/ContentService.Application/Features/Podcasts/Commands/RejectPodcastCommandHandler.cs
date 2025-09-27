using MediatR;
using ContentService.Domain.Interfaces;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.EventBus;

namespace ContentService.Application.Features.Podcasts.Commands;

public class RejectPodcastCommandHandler : IRequestHandler<RejectPodcastCommand, bool>
{
    private readonly IContentRepository _repository;
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;

    public RejectPodcastCommandHandler(IContentRepository repository, IOutboxUnitOfWork outboxUnitOfWork)
    {
        _repository = repository;
        _outboxUnitOfWork = outboxUnitOfWork;
    }

    public async Task<bool> Handle(RejectPodcastCommand request, CancellationToken cancellationToken)
    {
        var podcast = await _repository.GetPodcastByIdAsync(request.Id, cancellationToken);
        
        if (podcast == null)
            return false;

        // Update podcast status
        podcast.ContentStatus = ContentStatus.Rejected;

        await _repository.UpdatePodcastAsync(podcast, cancellationToken);

        // Publish domain event
        var domainEvent = new PodcastRejectedEvent
        {
            Id = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow,
            PodcastId = podcast.Id,
            CreatorId = podcast.CreatedBy ?? Guid.Empty,
            ModeratorId = request.ModeratorId,
            RejectionReason = request.RejectionReason,
            Title = podcast.Title
        };

        await _outboxUnitOfWork.AddOutboxEventAsync(domainEvent);
        await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

        return true;
    }
}

public record PodcastRejectedEvent : IntegrationEvent
{
    public Guid PodcastId { get; init; }
    public Guid CreatorId { get; init; }
    public Guid ModeratorId { get; init; }
    public string RejectionReason { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}