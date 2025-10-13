using AutoMapper;
using PaymentService.Application.Commons.DTOs;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Commons.Mappings;

public class PaymentMethodMappingProfile : Profile
{
    public PaymentMethodMappingProfile()
    {
        // PaymentMethod to Response
        CreateMap<PaymentMethod, PaymentMethodResponse>()
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // Request to PaymentMethod (for Create)
        CreateMap<PaymentMethodRequest, PaymentMethod>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());
    }
}

