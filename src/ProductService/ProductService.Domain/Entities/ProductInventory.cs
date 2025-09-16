using ProductAuthMicroservice.Commons.Entities;

namespace ProductAuthMicroservice.ProductService.Domain.Entities;

public class ProductInventory : BaseEntity
{
    public int Quantity { get; set; }
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
}