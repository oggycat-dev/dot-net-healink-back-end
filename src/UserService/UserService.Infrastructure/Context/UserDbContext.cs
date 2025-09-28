using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Context;

/// <summary>
/// DbContext cho User Service với tất cả business entities
/// </summary>
public class UserDbContext : DbContext
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<BusinessRole> BusinessRoles { get; set; }
    public DbSet<UserBusinessRole> UserBusinessRoles { get; set; }
    public DbSet<CreatorApplication> CreatorApplications { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure UserProfile entity
        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasIndex(x => x.UserId).IsUnique(); // One profile per user
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.PhoneNumber);
            entity.HasIndex(x => new { x.UserId, x.LastLoginAt });
            
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.AvatarPath).HasMaxLength(500);
            
            entity.Property(x => x.LastLoginAt)
                .HasColumnType("timestamp with time zone");
        });

        // Configure BusinessRole entity
        builder.Entity<BusinessRole>(entity =>
        {
            entity.ToTable("BusinessRoles");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.RoleType).IsUnique();
            entity.HasIndex(x => x.Priority);
            entity.HasIndex(x => new { x.IsActive, x.Priority });
            
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.RoleType).HasConversion<int>();
            entity.Property(x => x.RequiredCoreRole).HasConversion<int>();
            entity.Property(x => x.Permissions).HasColumnType("jsonb").HasDefaultValue("[]");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.Priority).HasDefaultValue(999);
        });

        // Configure UserBusinessRole entity (Many-to-Many with metadata)
        builder.Entity<UserBusinessRole>(entity =>
        {
            entity.ToTable("UserBusinessRoles");
            entity.HasIndex(x => new { x.UserId, x.BusinessRoleId }).IsUnique();
            entity.HasIndex(x => x.AssignedAt);
            entity.HasIndex(x => x.ExpiresAt).HasFilter("\"ExpiresAt\" IS NOT NULL");
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            
            // Foreign key relationships - UserId references UserProfile.Id (Primary Key)
            entity.HasOne(x => x.User)
                .WithMany(x => x.UserBusinessRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(x => x.BusinessRole)
                .WithMany(x => x.UserBusinessRoles)
                .HasForeignKey(x => x.BusinessRoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Property(x => x.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone");
                
            entity.Property(x => x.ExpiresAt)
                .HasColumnType("timestamp with time zone");
                
            entity.Property(x => x.Notes).HasMaxLength(1000);
        });

        // Configure CreatorApplication entity
        builder.Entity<CreatorApplication>(entity =>
        {
            entity.ToTable("CreatorApplications");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.SubmittedAt);
            entity.HasIndex(x => x.ReviewedAt).HasFilter("\"ReviewedAt\" IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.SubmittedAt });
            
            // Foreign key relationships - UserId references UserProfile.Id (Primary Key)
            entity.HasOne(x => x.User)
                .WithMany(x => x.CreatorApplications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(x => x.RequestedBusinessRole)
                .WithMany()
                .HasForeignKey(x => x.RequestedBusinessRoleId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany(x => x.ReviewedApplications)
                .HasForeignKey(x => x.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.Property(x => x.ApplicationData)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
                
            entity.Property(x => x.Status).HasConversion<int>();
            
            entity.Property(x => x.SubmittedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone");
                
            entity.Property(x => x.ReviewedAt)
                .HasColumnType("timestamp with time zone");
                
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
            entity.Property(x => x.ReviewNotes).HasMaxLength(2000);
        });

        // Configure UserActivityLog entity
        builder.Entity<UserActivityLog>(entity =>
        {
            entity.ToTable("UserActivityLogs");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.ActivityType);
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => new { x.UserId, x.OccurredAt });
            entity.HasIndex(x => new { x.ActivityType, x.OccurredAt });
            
            // Foreign key relationship - UserId references UserProfile.Id (Primary Key)
            entity.HasOne(x => x.User)
                .WithMany(x => x.ActivityLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Property(x => x.ActivityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Metadata).HasColumnType("jsonb").HasDefaultValue("{}");
            entity.Property(x => x.IpAddress).HasMaxLength(45); // IPv6 max length
            entity.Property(x => x.UserAgent).HasMaxLength(1000);
            
            entity.Property(x => x.OccurredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone");
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

        // Apply shared configurations
        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
        
        // UserService không cần Saga entities - chỉ là consumer
        // Saga tables chỉ có ở AuthService (Saga orchestrator)
        //builder.AddSagaEntities();
    }
}
