using MediatR;

namespace ContentService.Application.Features.Podcasts.Commands;

public record RejectPodcastCommand(
    Guid Id,
    Guid ModeratorId,
    string RejectionReason
) : IRequest<bool>;