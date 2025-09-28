using MediatR;
using ContentService.Domain.Interfaces;
using AutoMapper;

namespace ContentService.Application.Features.Podcasts.Queries;

public class GetPodcastByIdQueryHandler : IRequestHandler<GetPodcastByIdQuery, PodcastDto?>
{
    private readonly IContentRepository _repository;
    private readonly IMapper _mapper;

    public GetPodcastByIdQueryHandler(IContentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PodcastDto?> Handle(GetPodcastByIdQuery request, CancellationToken cancellationToken)
    {
        var podcast = await _repository.GetPodcastByIdAsync(request.Id, cancellationToken);
        
        if (podcast == null)
            return null;

        return _mapper.Map<PodcastDto>(podcast);
    }
}