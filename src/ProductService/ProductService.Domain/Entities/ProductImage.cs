using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.ProductService.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    
    // navigation property
    public virtual Product Product { get; set; } = null!;
}
