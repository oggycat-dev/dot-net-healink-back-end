using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;
using SubscriptionService.Infrastructure.Extensions;

namespace SubscriptionService.Infrastructure.Context;

public class SubscriptionDbContext : DbContext
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : base(options)
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

        builder.Entity<SubscriptionPlan>(e =>
        {
            e.ToTable("SubscriptionPlans");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.FeatureConfig).HasColumnType("jsonb").HasDefaultValue("{}");
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.BillingPeriodCount).HasDefaultValue(1);
            e.Property(x => x.TrialDays).HasDefaultValue(0);
            e.Property(x => x.BillingPeriodUnit).HasConversion<int>();
        });

        builder.Entity<Subscription>(e =>
        {
            e.ToTable("Subscriptions");
            e.HasIndex(x => x.UserProfileId);
            e.HasIndex(x => new { x.CurrentPeriodEnd });
            e.Property(x => x.RenewalBehavior).HasConversion<int>();
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.SubscriptionPlanId);
            // Enforce at most one active subscription per user
            e.HasIndex(x => new { x.UserProfileId, x.SubscriptionStatus })
                .HasFilter("\"SubscriptionStatus\" = 2") // Active = 2
                .IsUnique();
        });

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
        
        // Add Subscription Saga entities
        builder.AddSubscriptionSagaEntities();
    }
}

