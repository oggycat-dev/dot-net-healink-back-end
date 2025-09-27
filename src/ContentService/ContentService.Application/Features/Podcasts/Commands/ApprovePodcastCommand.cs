using MediatR;

namespace ContentService.Application.Features.Podcasts.Commands;

public record ApprovePodcastCommand(
    Guid Id,
    Guid ModeratorId,
    string? ApprovalNotes
) : IRequest<ApprovePodcastResponse>;

public record ApprovePodcastResponse(
    bool Success,
    string Message,
    Guid? PodcastId = null
);