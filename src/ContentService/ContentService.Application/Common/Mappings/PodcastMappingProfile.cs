using AutoMapper;
using ContentService.Domain.Entities;
using ContentService.Application.Features.Podcasts.Queries;
using ContentService.Application.Features.Podcasts.Commands;

namespace ContentService.Application.Common.Mappings;

public class PodcastMappingProfile : Profile
{
    public PodcastMappingProfile()
    {
        CreateMap<Podcast, PodcastDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.ThumbnailUrl))
            .ForMember(dest => dest.AudioUrl, opt => opt.MapFrom(src => src.AudioUrl))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
            .ForMember(dest => dest.TranscriptUrl, opt => opt.MapFrom(src => src.TranscriptUrl))
            .ForMember(dest => dest.HostName, opt => opt.MapFrom(src => src.HostName))
            .ForMember(dest => dest.GuestName, opt => opt.MapFrom(src => src.GuestName))
            .ForMember(dest => dest.EpisodeNumber, opt => opt.MapFrom(src => src.EpisodeNumber))
            .ForMember(dest => dest.SeriesName, opt => opt.MapFrom(src => src.SeriesName))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags ?? new string[0]))
            .ForMember(dest => dest.EmotionCategories, opt => opt.MapFrom(src => src.EmotionCategories ?? new Domain.Enums.EmotionCategory[0]))
            .ForMember(dest => dest.TopicCategories, opt => opt.MapFrom(src => src.TopicCategories ?? new Domain.Enums.TopicCategory[0]))
            .ForMember(dest => dest.ContentStatus, opt => opt.MapFrom(src => src.ContentStatus))
            .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.ViewCount))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikeCount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.PublishedAt, opt => opt.MapFrom(src => src.PublishedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
    }
}