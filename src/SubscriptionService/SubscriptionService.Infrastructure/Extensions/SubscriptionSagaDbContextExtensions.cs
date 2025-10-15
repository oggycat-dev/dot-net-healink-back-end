using MassTransit;
using Microsoft.EntityFrameworkCore;
using SubscriptionService.Infrastructure.Saga;

namespace SubscriptionService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for adding SubscriptionService Saga entities to DbContext
/// </summary>
public static class SubscriptionSagaDbContextExtensions
{
    /// <summary>
    /// Add SubscriptionService Saga entities to DbContext
    /// </summary>
    public static void AddSubscriptionSagaEntities(this ModelBuilder modelBuilder)
    {
        // Configure RegisterSubscriptionSagaState with optimized indexing strategy
        modelBuilder.Entity<RegisterSubscriptionSagaState>(entity =>
        {
            // Primary key - CorrelationId maps to SubscriptionId
            entity.HasKey(e => e.CorrelationId);
            
            // Optimistic concurrency control using Version (ISagaVersion)
            entity.Property(e => e.Version)
                .IsConcurrencyToken()
                .IsRequired();
            
            // Column configurations
            entity.Property(e => e.CurrentState).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SubscriptionPlanName).HasMaxLength(200);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.PaymentProvider).HasMaxLength(100);
            entity.Property(e => e.TransactionId).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
            
            // PERFORMANCE INDEXES
            
            // 1. User lookup index - for finding sagas by user
            entity.HasIndex(e => e.UserProfileId)
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_UserProfileId");
            
            // 2. Current state index - for filtering by state
            entity.HasIndex(e => e.CurrentState)
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_CurrentState");
            
            // 3. Payment tracking index
            entity.HasIndex(e => e.PaymentIntentId)
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_PaymentIntentId")
                .HasFilter("\"PaymentIntentId\" IS NOT NULL");
            
            // 4. Composite index for user + state + created time
            entity.HasIndex(e => new { e.UserProfileId, e.CurrentState, e.StartedAt })
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_User_State_Started");
            
            // 5. Temporal index for cleanup operations
            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_StartedAt");
            
            // 6. Completion status index for monitoring
            entity.HasIndex(e => new { e.IsPaymentCompleted, e.IsSubscriptionActivated, e.IsFailed })
                .HasDatabaseName("IX_RegisterSubscriptionSagaStates_Status");
            
            // Table name
            entity.ToTable("RegisterSubscriptionSagaStates");
        });
    }
}

