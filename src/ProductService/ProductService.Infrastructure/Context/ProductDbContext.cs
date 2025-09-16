using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.ProductService.Domain.Entities;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Outbox;

namespace ProductService.Infrastructure.Context;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductInventory> ProductInventories { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>(entity =>
        {
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(x => x.Price).HasColumnType("decimal(10,2)");
            entity.Property(x => x.DiscountPrice).HasColumnType("decimal(10,2)");

            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.IsPreOrder);
            entity.HasIndex(x => x.PreOrderReleaseDate).HasFilter("\"PreOrderReleaseDate\" IS NOT NULL");
            entity.HasIndex(x => x.Price);
            entity.HasIndex(x => x.StockQuantity);
        });

        // ProductInventory
        builder.Entity<ProductInventory>(entity =>
        {
            entity.HasOne(x => x.Product)
                .WithMany(x => x.ProductInventories)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ProductId);
        });

        // ProductImage
        builder.Entity<ProductImage>(entity =>
        {
            entity.HasOne(x => x.Product)
                .WithMany(x => x.ProductImages)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.IsPrimary);
            entity.HasIndex(x => x.DisplayOrder);
        });

        // Configure OutboxEvent
        builder.Entity<OutboxEvent>(entity =>
        {
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.ProcessedAt);
            entity.HasIndex(x => x.NextRetryAt);
            entity.HasIndex(x => new { x.ProcessedAt, x.NextRetryAt, x.RetryCount });
            entity.Property(x => x.EventData).HasColumnType("text");
        });

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
    }
}
