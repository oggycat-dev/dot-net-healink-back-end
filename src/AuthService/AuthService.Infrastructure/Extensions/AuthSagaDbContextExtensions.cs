using MassTransit;
using Microsoft.EntityFrameworkCore;
using AuthService.Infrastructure.Saga;

namespace AuthService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for adding AuthService Saga entities to DbContext
/// </summary>
public static class AuthSagaDbContextExtensions
{
    /// <summary>
    /// Add AuthService Saga entities to DbContext
    /// </summary>
    public static void AddAuthSagaEntities(this ModelBuilder modelBuilder)
    {
        // Add MassTransit infrastructure entities (inbox, outbox)
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
        
        // Configure AdminUserCreationSagaState with same indexing strategy
        modelBuilder.Entity<AdminUserCreationSagaState>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.CorrelationId);
            
            // Column configurations
            entity.Property(e => e.CurrentState).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EncryptedPassword).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired(false);
            entity.Property(e => e.Address).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000).IsRequired(false);
            
            // Role enum stored as integer
            entity.Property(e => e.Role).HasConversion<int>().IsRequired();
            
            // PERFORMANCE INDEXES - Same pattern as RegistrationSagaState
            
            // 1. Email lookup index
            entity.HasIndex(e => e.Email)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_Email");
            
            // 2. Current state index
            entity.HasIndex(e => e.CurrentState)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_CurrentState");
            
            // 3. Composite index for email + state + created time
            entity.HasIndex(e => new { e.Email, e.CurrentState, e.CreatedAt })
                .HasDatabaseName("IX_AdminUserCreationSagaStates_Email_State_Created");
            
            // 4. Temporal index for cleanup operations
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_CreatedAt");
            
            // 5. Completion status index
            entity.HasIndex(e => new { e.IsCompleted, e.IsFailed })
                .HasDatabaseName("IX_AdminUserCreationSagaStates_Status");
            
            // 6. Time-based indexes
            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_StartedAt");
            
            entity.HasIndex(e => e.CompletedAt)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_CompletedAt");
            
            // 7. UserProfile and AuthUser lookup indexes
            entity.HasIndex(e => e.UserProfileId)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_UserProfileId");
            
            entity.HasIndex(e => e.AuthUserId)
                .HasDatabaseName("IX_AdminUserCreationSagaStates_AuthUserId");
            
            // Table name
            entity.ToTable("AdminUserCreationSagaStates");
        });
    }
}
