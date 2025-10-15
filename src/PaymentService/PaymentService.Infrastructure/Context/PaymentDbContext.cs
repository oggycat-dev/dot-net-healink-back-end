using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Context;

public class PaymentDbContext : DbContext
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // CRITICAL: Add MassTransit Outbox entities
        // Reference: https://masstransit.io/documentation/configuration/middleware/outbox
        builder.AddInboxStateEntity();      // For deduplication (consumer-side)
        builder.AddOutboxMessageEntity();   // For storing published messages
        builder.AddOutboxStateEntity();     // For tracking bus outbox delivery

        // PaymentMethod configuration
        builder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ProviderName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Configuration).HasColumnType("jsonb"); // PostgreSQL JSON
            entity.Property(e => e.Type).HasConversion<int>().IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
        });

        // PaymentTransaction configuration
        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            
            entity.Property(e => e.Amount).HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.TransactionId).HasMaxLength(200);
            entity.Property(e => e.ErrorCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.TransactionType).HasConversion<int>().IsRequired();
            
            // Indexes for performance
            entity.HasIndex(e => new { e.TransactionType, e.ReferenceId });
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CreatedAt); // For temporal queries
            
            // Foreign key relationship
            entity.HasOne(e => e.PaymentMethod)
                .WithMany()
                .HasForeignKey(e => e.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
    }
}

