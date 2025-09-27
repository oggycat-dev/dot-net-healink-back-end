using MediatR;
using Microsoft.Extensions.Logging;
using ContentService.Application.Features.Podcasts.Commands;
using ContentService.Application.Events;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Interfaces;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Services;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Application.Features.Podcasts.Handlers;

public class CreatePodcastCommandHandler : IRequestHandler<CreatePodcastCommand, CreatePodcastResponse>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePodcastCommandHandler> _logger;

    public CreatePodcastCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<CreatePodcastCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CreatePodcastResponse> Handle(CreatePodcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user
            if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            _logger.LogInformation("Creating podcast with title: {Title} for user: {UserId}", 
                request.Title, userId);

            // Upload audio file to S3
            var audioUrl = await _fileStorageService.UploadFileAsync(request.AudioFile, "podcasts/audio");
            _logger.LogInformation("Audio file uploaded successfully: {AudioUrl}", audioUrl);

            // Upload thumbnail if provided
            string? thumbnailUrl = null;
            if (request.ThumbnailFile != null)
            {
                thumbnailUrl = await _fileStorageService.UploadFileAsync(request.ThumbnailFile, "podcasts/thumbnails");
                _logger.LogInformation("Thumbnail uploaded successfully: {ThumbnailUrl}", thumbnailUrl);
            }

            // Create podcast entity
            var podcast = new Podcast
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                ThumbnailUrl = thumbnailUrl,
                AudioUrl = audioUrl,
                Duration = request.Duration,
                TranscriptUrl = request.TranscriptUrl,
                HostName = request.HostName,
                GuestName = request.GuestName,
                EpisodeNumber = request.EpisodeNumber,
                SeriesName = request.SeriesName,
                Tags = request.Tags,
                EmotionCategories = request.EmotionCategories,
                TopicCategories = request.TopicCategories,
                ContentStatus = ContentStatus.PendingModeration,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            await _outboxUnitOfWork.Repository<Podcast>().AddAsync(podcast);

            // Create podcast-specific creation event
            var podcastCreatedEvent = new PodcastCreatedEvent(
                podcast.Id,
                podcast.Title,
                podcast.Description,
                podcast.AudioUrl,
                podcast.Duration,
                userId,
                podcast.CreatedAt ?? DateTime.UtcNow,
                podcast.Tags,
                podcast.EmotionCategories,
                podcast.TopicCategories
            );

            // Create general content creation event
            var contentCreatedEvent = new ContentCreatedEvent(
                podcast.Id,
                podcast.Title,
                podcast.Description,
                podcast.ContentType,
                podcast.ContentStatus,
                userId,
                podcast.CreatedAt ?? DateTime.UtcNow,
                podcast.Tags
            );

            // Add outbox events
            await _outboxUnitOfWork.AddOutboxEventAsync(podcastCreatedEvent);
            await _outboxUnitOfWork.AddOutboxEventAsync(contentCreatedEvent);

            // Save changes with outbox
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Podcast created successfully with ID: {PodcastId}", podcast.Id);

            return new CreatePodcastResponse(
                podcast.Id,
                podcast.Title,
                podcast.AudioUrl,
                podcast.ThumbnailUrl,
                podcast.ContentStatus,
                podcast.CreatedAt.Value
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating podcast with title: {Title}", request.Title);
            throw;
        }
    }
}

public class UpdatePodcastCommandHandler : IRequestHandler<UpdatePodcastCommand, UpdatePodcastResponse>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdatePodcastCommandHandler> _logger;

    public UpdatePodcastCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdatePodcastCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdatePodcastResponse> Handle(UpdatePodcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var podcast = await _outboxUnitOfWork.Repository<Podcast>()
                .GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            if (podcast == null)
            {
                throw new InvalidOperationException($"Podcast with ID {request.Id} not found");
            }

            // Update podcast properties
            podcast.Title = request.Title;
            podcast.Description = request.Description;
            podcast.ThumbnailUrl = request.ThumbnailUrl;
            podcast.AudioUrl = request.AudioUrl;
            podcast.Duration = request.Duration;
            podcast.TranscriptUrl = request.TranscriptUrl;
            podcast.HostName = request.HostName;
            podcast.GuestName = request.GuestName;
            podcast.EpisodeNumber = request.EpisodeNumber;
            podcast.SeriesName = request.SeriesName;
            podcast.Tags = request.Tags;
            podcast.EmotionCategories = request.EmotionCategories;
            podcast.TopicCategories = request.TopicCategories;
            podcast.UpdatedAt = DateTime.UtcNow;

            _outboxUnitOfWork.Repository<Podcast>().Update(podcast);
            
            // Create content updated event
            var contentUpdatedEvent = new ContentUpdatedEvent(
                podcast.Id,
                podcast.Title,
                podcast.Description,
                podcast.ContentType,
                podcast.ContentStatus,
                Guid.Parse(_currentUserService.UserId!),
                podcast.UpdatedAt.Value,
                podcast.Tags
            );

            // Add outbox event
            await _outboxUnitOfWork.AddOutboxEventAsync(contentUpdatedEvent);
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Podcast updated successfully with ID: {PodcastId}", podcast.Id);

            return new UpdatePodcastResponse(
                podcast.Id,
                podcast.Title,
                podcast.UpdatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating podcast with ID: {PodcastId}", request.Id);
            throw;
        }
    }
}

public class DeletePodcastCommandHandler : IRequestHandler<DeletePodcastCommand, bool>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeletePodcastCommandHandler> _logger;

    public DeletePodcastCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<DeletePodcastCommandHandler> logger)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeletePodcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user
            if (_currentUserService.UserId == null || !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            var podcast = await _outboxUnitOfWork.Repository<Podcast>()
                .GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
            if (podcast == null)
            {
                return false;
            }

            // Check ownership - users can only delete their own content
            if (podcast.CreatedBy != userId)
            {
                throw new UnauthorizedAccessException("You can only delete your own content");
            }

            _logger.LogInformation("Deleting podcast with ID: {PodcastId} and title: {Title}", 
                podcast.Id, podcast.Title);

            // Delete audio file from S3
            if (!string.IsNullOrEmpty(podcast.AudioUrl))
            {
                var audioDeleted = await _fileStorageService.DeleteFileAsync(podcast.AudioUrl);
                if (audioDeleted)
                {
                    _logger.LogInformation("Audio file deleted from S3: {AudioUrl}", podcast.AudioUrl);
                }
                else
                {
                    _logger.LogWarning("Failed to delete audio file from S3: {AudioUrl}", podcast.AudioUrl);
                }
            }

            // Delete thumbnail file from S3 if exists
            if (!string.IsNullOrEmpty(podcast.ThumbnailUrl))
            {
                var thumbnailDeleted = await _fileStorageService.DeleteFileAsync(podcast.ThumbnailUrl);
                if (thumbnailDeleted)
                {
                    _logger.LogInformation("Thumbnail file deleted from S3: {ThumbnailUrl}", podcast.ThumbnailUrl);
                }
                else
                {
                    _logger.LogWarning("Failed to delete thumbnail file from S3: {ThumbnailUrl}", podcast.ThumbnailUrl);
                }
            }

            // Soft delete (handled by BaseEntity)
            _outboxUnitOfWork.Repository<Podcast>().Delete(podcast);

            // Create integration event for content deletion
            var contentDeletedEvent = new ContentDeletedEvent(
                podcast.Id,
                podcast.Title,
                podcast.ContentType,
                podcast.CreatedBy.Value,
                request.DeletedBy,
                DateTime.UtcNow
            );

            // Add outbox event
            await _outboxUnitOfWork.AddOutboxEventAsync(contentDeletedEvent);

            // Save changes with outbox
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            _logger.LogInformation("Podcast deleted successfully with ID: {PodcastId}", podcast.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting podcast with ID: {PodcastId}", request.Id);
            throw;
        }
    }
}