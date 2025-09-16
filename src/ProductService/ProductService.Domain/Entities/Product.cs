using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.ProductService.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public Guid CategoryId { get; set; }
    public bool IsPreOrder { get; set; } = false;
    public DateTime? PreOrderReleaseDate { get; set; }
    
    // navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<ProductInventory> ProductInventories { get; set; } = new List<ProductInventory>();
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}