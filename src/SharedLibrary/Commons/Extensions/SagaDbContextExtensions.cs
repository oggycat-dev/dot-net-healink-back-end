using MassTransit;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts.User.Saga;

namespace SharedLibrary.Commons.Extensions;

/// <summary>
/// Extension methods for adding MassTransit Saga entities to DbContext
/// </summary>
public static class SagaDbContextExtensions
{
    /// <summary>
    /// Add MassTransit Saga entities to DbContext
    /// </summary>
    public static void AddSagaEntities(this ModelBuilder modelBuilder)
    {
        // Add RegistrationSagaState mapping
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        
        // Configure RegistrationSagaState with optimized indexing strategy
        modelBuilder.Entity<RegistrationSagaState>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.CorrelationId);
            
            // Column configurations - Make nullable fields optional for saga initialization
            entity.Property(e => e.CurrentState).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EncryptedPassword).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired(false);
            entity.Property(e => e.OtpCode).HasMaxLength(10).IsRequired(false);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000).IsRequired(false);
            
            // PERFORMANCE INDEXES - No unique constraints to allow multiple records per email
            
            // 1. Email lookup index - for finding sagas by email (allows duplicates)
            entity.HasIndex(e => e.Email)
                .HasDatabaseName("IX_RegistrationSagaStates_Email");
            
            // 2. Current state index - for filtering by state
            entity.HasIndex(e => e.CurrentState)
                .HasDatabaseName("IX_RegistrationSagaStates_CurrentState");
            
            // 3. Composite index for email + state + created time - for efficient queries
            entity.HasIndex(e => new { e.Email, e.CurrentState, e.CreatedAt })
                .HasDatabaseName("IX_RegistrationSagaStates_Email_State_Created");
            
            // 4. Temporal index for cleanup operations
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_RegistrationSagaStates_CreatedAt");
            
            // 5. Completion status index for monitoring
            entity.HasIndex(e => new { e.IsCompleted, e.IsFailed })
                .HasDatabaseName("IX_RegistrationSagaStates_Status");
            
            // 6. Time-based indexes for efficient range queries
            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_RegistrationSagaStates_StartedAt");
            
            entity.HasIndex(e => e.CompletedAt)
                .HasDatabaseName("IX_RegistrationSagaStates_CompletedAt");
            
            // Table name
            entity.ToTable("RegistrationSagaStates");
        });
    }
}