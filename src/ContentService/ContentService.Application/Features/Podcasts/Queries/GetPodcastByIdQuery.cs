using MediatR;

namespace ContentService.Application.Features.Podcasts.Queries;

public record GetPodcastByIdQuery(Guid Id) : IRequest<PodcastDto?>;