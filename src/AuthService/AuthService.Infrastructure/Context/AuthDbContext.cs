using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;

namespace AuthService.Infrastructure.Context;

public class AuthDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Đổi tên bảng Identity
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.Entity<AppUser>(entity =>
        {
            // Performance indexes
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.LastLoginAt).HasFilter("\"LastLoginAt\" IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
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

        // Configure the base IdentityUserRole<Guid> key
        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        // Configure Permission entity
        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Module);
            entity.HasIndex(x => new { x.Module, x.Name });
            
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Module).HasConversion<int>();
        });

        // Configure RolePermission entity
        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            entity.HasIndex(x => x.AssignedAt);
            
            // Foreign key relationships
            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Property(x => x.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone");
        });

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
        
        // Add Saga entities for MassTransit
        builder.AddSagaEntities();
    }
}