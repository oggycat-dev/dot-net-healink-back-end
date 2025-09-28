using MediatR;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.Application.Events;
using ContentService.Domain.Interfaces;
using ContentService.Domain.Enums;
using Microsoft.Extensions.Logging;
using AutoMapper;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.EventBus;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class ApprovePodcastCommandHandler : IRequestHandler<ApprovePodcastCommand, ApprovePodcastResponse>
{
    private readonly IContentRepository _repository;
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<ApprovePodcastCommandHandler> _logger;
    private readonly IMapper _mapper;

    public ApprovePodcastCommandHandler(
        IContentRepository repository,
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<ApprovePodcastCommandHandler> logger,
        IMapper mapper)
    {
        _repository = repository;
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApprovePodcastResponse> Handle(ApprovePodcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var podcast = await _repository.GetPodcastByIdAsync(request.Id, cancellationToken);
            
            if (podcast == null)
            {
                throw new InvalidOperationException($"Podcast with ID {request.Id} not found");
            }

            // Update podcast status to approved and published
            podcast.ContentStatus = ContentStatus.Published;
            podcast.ApprovedBy = request.ModeratorId;
            podcast.ApprovedAt = DateTime.UtcNow;
            podcast.PublishedAt = DateTime.UtcNow;

            await _repository.UpdatePodcastAsync(podcast, cancellationToken);

            // Create content approval event
            var contentApprovedEvent = new ContentApprovedEvent(
                podcast.Id,
                podcast.Title,
                podcast.ContentType,
                podcast.CreatedBy ?? Guid.Empty,
                request.ModeratorId,
                podcast.ApprovedAt.Value,
                request.ApprovalNotes
            );

            // Create podcast published event
            var podcastPublishedEvent = new PodcastPublishedEvent(
                podcast.Id,
                podcast.Title,
                podcast.Description,
                podcast.AudioUrl!,
                podcast.Duration,
                podcast.CreatedBy ?? Guid.Empty,
                request.ModeratorId,
                podcast.PublishedAt.Value,
                podcast.Tags,
                podcast.EmotionCategories,
                podcast.TopicCategories
            );

            // Add outbox events
            await _outboxUnitOfWork.AddOutboxEventAsync(contentApprovedEvent);
            await _outboxUnitOfWork.AddOutboxEventAsync(podcastPublishedEvent);
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Podcast approved successfully with ID: {PodcastId} by moderator: {ModeratorId}", 
                podcast.Id, request.ModeratorId);

            return new ApprovePodcastResponse(
                true,
                "Podcast approved and published successfully",
                podcast.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving podcast with ID: {PodcastId}", request.Id);
            throw;
        }
    }
}
