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
        
        // Configure RegistrationSagaState
        modelBuilder.Entity<RegistrationSagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.Property(e => e.CurrentState).HasMaxLength(64);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EncryptedPassword).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.OtpCode).HasMaxLength(10);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // Add indexes for performance
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.CurrentState);
            entity.HasIndex(e => e.CreatedAt);
            
            // Table name
            entity.ToTable("RegistrationSagaStates");
        });
    }
}