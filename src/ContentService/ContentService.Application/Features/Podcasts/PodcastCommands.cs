using MediatR;
using ContentService.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ContentService.Application.Features.Podcasts.Commands;

public record CreatePodcastCommand(
    string Title,
    string Description,
    IFormFile AudioFile,
    TimeSpan Duration,
    string? TranscriptUrl,
    string? HostName,
    string? GuestName,
    int EpisodeNumber,
    string? SeriesName,
    string[]? Tags,
    EmotionCategory[]? EmotionCategories,
    TopicCategory[]? TopicCategories,
    IFormFile? ThumbnailFile = null
) : IRequest<CreatePodcastResponse>;

public record CreatePodcastResponse(
    Guid Id,
    string Title,
    string AudioUrl,
    string? ThumbnailUrl,
    ContentStatus ContentStatus,
    DateTime? CreatedAt
);

public record UpdatePodcastCommand(
    Guid Id,
    string? Title,
    string? Description,
    TimeSpan? Duration,
    string? TranscriptUrl,
    string? HostName,
    string? GuestName,
    int? EpisodeNumber,
    string? SeriesName,
    string[]? Tags,
    EmotionCategory[]? EmotionCategories,
    TopicCategory[]? TopicCategories,
    IFormFile? AudioFile,
    IFormFile? ThumbnailFile,
    Guid UpdatedBy
) : IRequest<UpdatePodcastResponse>;

public record UpdatePodcastResponse(
    Guid Id,
    string Title,
    DateTime? UpdatedAt
);

public record DeletePodcastCommand(Guid Id, Guid DeletedBy) : IRequest<bool>;

public record IncrementPodcastViewCommand(Guid Id) : IRequest;

public record TogglePodcastLikeCommand(Guid Id, Guid UserId) : IRequest<int>;