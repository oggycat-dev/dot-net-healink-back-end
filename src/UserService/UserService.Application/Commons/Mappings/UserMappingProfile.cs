using AutoMapper;
using UserService.Application.Commons.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Commons.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserProfile, ProfileResponse>();
        
        // ✅ Mapping for UserProfileResponse (roles will be populated separately via RPC)
        CreateMap<UserProfile, UserProfileResponse>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles filled by handler after RPC call
    }
}
