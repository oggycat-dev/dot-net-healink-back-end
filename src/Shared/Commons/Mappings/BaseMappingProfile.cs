using AutoMapper;
using ProductAuthMicroservice.Commons.Entities;
using ProductAuthMicroservice.Commons.Models;

namespace ProductAuthMicroservice.Commons.Mappings;

public abstract class BaseMappingProfile : Profile
{
   protected BaseMappingProfile()
   {
        CreateMap<BaseEntity, BaseResponse>();
   }
}
