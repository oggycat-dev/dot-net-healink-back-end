using AutoMapper;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Models;

namespace SharedLibrary.Commons.Mappings;

public abstract class BaseMappingProfile : Profile
{
   protected BaseMappingProfile()
   {
        CreateMap<BaseEntity, BaseResponse>();
   }
}
