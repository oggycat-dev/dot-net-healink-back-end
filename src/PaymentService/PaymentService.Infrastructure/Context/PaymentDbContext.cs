using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Context;

public class PaymentDbContext : DbContext
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Invoice>(e =>
        {
            e.ToTable("Invoices");
            e.HasIndex(x => x.UserProfileId);
            e.HasIndex(x => new { x.Status, x.DueDate });
            e.HasIndex(x => x.CorrelationId).IsUnique();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
            e.Property(x => x.Tax).HasColumnType("numeric(18,2)");
            e.Property(x => x.Total).HasColumnType("numeric(18,2)");
            e.Property(x => x.Discounts).HasColumnType("jsonb").HasDefaultValue("[]");
        });

        builder.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("PaymentTransactions");
            e.HasIndex(x => new { x.InvoiceId, x.Status });
            e.HasIndex(x => new { x.TransactionType, x.ReferenceId });
            e.HasIndex(x => x.ProviderChargeRef);
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.TransactionType).HasConversion<int>();
        });

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
    }
}

