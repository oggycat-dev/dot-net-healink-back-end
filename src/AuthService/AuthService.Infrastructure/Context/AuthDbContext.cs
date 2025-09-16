using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductAuthMicroservice.AuthService.Domain.Entities;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Outbox;

namespace AuthService.Infrastructure.Context;

public class AuthDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<UserAction> UserActions { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    
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
            entity.Property(x => x.Status).HasConversion<int>();
            // Performance indexes
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JoiningAt);
            entity.HasIndex(x => x.LastLoginAt).HasFilter("\"LastLoginAt\" IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.JoiningAt });
            // navigation property
            entity.HasMany(x => x.UserActions).WithOne(x => x.User).HasForeignKey(x => x.UserId);
        });

        builder.Entity<UserAction>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => x.EntityId);
        });

        builder.Entity<AppRole>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
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

        BaseEntityConfigExtension.ConfigureBaseEntities(builder);
    }
}
