using AutoMapper;
using ProductAuthMicroservice.Commons.Mappings;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;
using ProductAuthMicroservice.ProductService.Application.Features.ProductInventories.DTOs;
using ProductAuthMicroservice.ProductService.Domain.Entities;

namespace ProductAuthMicroservice.ProductService.Application.Mappings;

public class ProductMappingProfile : BaseMappingProfile
{
    public ProductMappingProfile()
    {
        // Product mappings
        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Category, opt => opt.Ignore()) // Handled manually
            .ForMember(dest => dest.Images, opt => opt.Ignore()); // Handled manually

        CreateMap<Product, ProductItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : ""));

        // DTO to Entity mappings
        CreateMap<ProductRequestDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.ProductInventories, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImages, opt => opt.Ignore());

        // Category mappings for Product context
        CreateMap<Category, ProductCategoryDto>();

        // ProductImage mappings
        CreateMap<ProductImage, ProductImageResponseDto>();

        // ProductInventory mappings
        CreateMap<ProductInventory, ProductInventoryResponseDto>()
            .ForMember(dest => dest.ProductName, opt => opt.Ignore()) // Handled manually
            .ForMember(dest => dest.Note, opt => opt.Ignore()); // Not in domain model yet
    }
}
