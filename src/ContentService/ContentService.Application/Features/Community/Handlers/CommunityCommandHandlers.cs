using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Community.Commands;
using ContentService.Application.Events;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Application.Features.Community.Handlers;

public class CreateCommunityStoryCommandHandler : IRequestHandler<CreateCommunityStoryCommand, CreateCommunityStoryResponse>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<CreateCommunityStoryCommandHandler> _logger;

    public CreateCommunityStoryCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<CreateCommunityStoryCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
    }

    public async Task<CreateCommunityStoryResponse> Handle(CreateCommunityStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create community story entity
            var story = new CommunityStory
            {
                Title = request.Title,
                Description = request.Description,
                StoryContent = request.StoryContent,
                IsAnonymous = request.IsAnonymous,
                AuthorDisplayName = request.AuthorDisplayName,
                Tags = request.Tags,
                EmotionCategories = request.EmotionCategories,
                TopicCategories = request.TopicCategories,
                TriggerWarnings = request.TriggerWarnings,
                ContentStatus = ContentStatus.PendingModeration, // Community stories need moderation
                ContentType = ContentType.CommunityStory,
                CreatedBy = request.CreatedBy,
                Status = EntityStatusEnum.Active
            };

            // Initialize entity is handled by BaseEntity
            await _outboxUnitOfWork.Repository<CommunityStory>().AddAsync(story);

            // Create integration event for content creation
            var contentCreatedEvent = new ContentCreatedEvent(
                story.Id,
                story.Title,
                story.Description,
                story.ContentType,
                story.ContentStatus,
                story.CreatedBy.Value,
                story.CreatedAt!.Value,
                story.Tags
            );

            // Add outbox event
            await _outboxUnitOfWork.AddOutboxEventAsync(contentCreatedEvent);

            // Save changes with outbox
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Community story created successfully with ID: {StoryId}", story.Id);

            return new CreateCommunityStoryResponse(
                story.Id,
                story.Title,
                story.ContentStatus,
                story.CreatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating community story: {Title}", request.Title);
            throw;
        }
    }
}

public class ApproveCommunityStoryCommandHandler : IRequestHandler<ApproveCommunityStoryCommand, ApproveCommunityStoryResponse>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<ApproveCommunityStoryCommandHandler> _logger;

    public ApproveCommunityStoryCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<ApproveCommunityStoryCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
    }

    public async Task<ApproveCommunityStoryResponse> Handle(ApproveCommunityStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var story = await _outboxUnitOfWork.Repository<CommunityStory>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException($"Community story with ID {request.Id} not found");
            }

            // Update story status
            story.ContentStatus = ContentStatus.Published;
            story.IsModeratorPick = request.IsModeratorPick;
            story.ApprovedBy = request.ApprovedBy;
            story.PublishedAt = DateTime.UtcNow;

            _outboxUnitOfWork.Repository<CommunityStory>().Update(story);

            // Create integration event for content approval
            var contentPublishedEvent = new ContentPublishedEvent(
                story.Id,
                story.Title,
                story.ContentType,
                story.CreatedBy.Value,
                request.ApprovedBy,
                story.PublishedAt.Value
            );

            // Add outbox event
            await _outboxUnitOfWork.AddOutboxEventAsync(contentPublishedEvent);

            // Save changes with outbox
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Community story approved successfully with ID: {StoryId}", story.Id);

            return new ApproveCommunityStoryResponse(
                story.Id,
                story.ContentStatus,
                story.IsModeratorPick,
                story.PublishedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving community story with ID: {StoryId}", request.Id);
            throw;
        }
    }
}

public class RejectCommunityStoryCommandHandler : IRequestHandler<RejectCommunityStoryCommand, bool>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<RejectCommunityStoryCommandHandler> _logger;

    public RejectCommunityStoryCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ILogger<RejectCommunityStoryCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RejectCommunityStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var story = await _outboxUnitOfWork.Repository<CommunityStory>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
            if (story == null)
            {
                return false;
            }

            // Update story status
            story.ContentStatus = ContentStatus.Rejected;

            _outboxUnitOfWork.Repository<CommunityStory>().Update(story);

            // Create integration event for content rejection
            var contentRejectedEvent = new ContentRejectedEvent(
                story.Id,
                story.Title,
                story.ContentType,
                story.CreatedBy.Value,
                request.RejectedBy,
                request.Reason,
                DateTime.UtcNow
            );

            // Add outbox event
            await _outboxUnitOfWork.AddOutboxEventAsync(contentRejectedEvent);

            // Save changes with outbox
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Community story rejected successfully with ID: {StoryId}", story.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting community story with ID: {StoryId}", request.Id);
            throw;
        }
    }
}