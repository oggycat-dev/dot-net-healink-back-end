using AuthService.Application.Commons.DTOs;
using AuthService.Domain.Entities;
using AutoMapper;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Commons.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<RegisterRequest, AppUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => EntityStatusEnum.Active))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
    
}