using Microsoft.EntityFrameworkCore;
using ContentService.Domain.Entities;
using SharedLibrary.Commons.Extensions;
using SharedLibrary.Commons.Outbox;
using System.Text.Json;

namespace ContentService.Infrastructure.Context;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<Content> Contents { get; set; }
    public DbSet<Podcast> Podcasts { get; set; }
    public DbSet<Flashcard> Flashcards { get; set; }
    public DbSet<Postcard> Postcards { get; set; }
    public DbSet<LettersToMyself> LettersToMyself { get; set; }
    public DbSet<CommunityStory> CommunityStories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }
    public DbSet<ContentInteraction> ContentInteractions { get; set; }
    public DbSet<ContentRating> ContentRatings { get; set; }
    public DbSet<CreatorSettings> CreatorSettings { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Disable foreign key convention that auto-creates relationships
        // This prevents EF from creating CreatorSettingsId FK on Content table
        configurationBuilder.Conventions.Remove(typeof(Microsoft.EntityFrameworkCore.Metadata.Conventions.ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply BaseEntity configurations
        BaseEntityConfigExtension.ConfigureBaseEntities(modelBuilder);

        // Configure Content base entity
        modelBuilder.Entity<Content>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            // Array properties stored as JSON
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
                    
            entity.Property(e => e.EmotionCategories)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Domain.Enums.EmotionCategory[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Domain.Enums.EmotionCategory>());
                    
            entity.Property(e => e.TopicCategories)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Domain.Enums.TopicCategory[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Domain.Enums.TopicCategory>());

            // Enum conversions
            entity.Property(e => e.ContentType).HasConversion<int>();
            entity.Property(e => e.ContentStatus).HasConversion<int>();

            // Default values for analytics
            entity.Property(e => e.ViewCount).HasDefaultValue(0);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.ShareCount).HasDefaultValue(0);
            entity.Property(e => e.CommentCount).HasDefaultValue(0);

            // Indexes for performance
            entity.HasIndex(e => e.ContentType);
            entity.HasIndex(e => e.ContentStatus);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PublishedAt);
        });

        // Configure Podcast entity
        modelBuilder.Entity<Podcast>(entity =>
        {
            entity.Property(e => e.AudioUrl).IsRequired();
            entity.Property(e => e.Duration).IsRequired();
            entity.Property(e => e.HostName).HasMaxLength(100);
            entity.Property(e => e.GuestName).HasMaxLength(100);
            entity.Property(e => e.SeriesName).HasMaxLength(100);
        });

        // Configure Flashcard entity
        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.Property(e => e.Question).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Answer).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Explanation).HasMaxLength(1000);
            entity.Property(e => e.DifficultyLevel).HasDefaultValue(1);
            
            entity.Property(e => e.RelatedPodcastIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
        });

        // Configure Postcard entity
        modelBuilder.Entity<Postcard>(entity =>
        {
            entity.Property(e => e.ImageUrl).IsRequired();
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.QuoteText).HasMaxLength(300);
            entity.Property(e => e.QuoteAuthor).HasMaxLength(100);
        });

        // Configure LettersToMyself entity
        modelBuilder.Entity<LettersToMyself>(entity =>
        {
            entity.Property(e => e.LetterContent).IsRequired();
            entity.Property(e => e.Mood).HasMaxLength(50);
            entity.Property(e => e.IsDelivered).HasDefaultValue(false);
            
            entity.Property(e => e.Intentions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
        });

        // Configure CommunityStory entity
        modelBuilder.Entity<CommunityStory>(entity =>
        {
            entity.Property(e => e.StoryContent).IsRequired();
            entity.Property(e => e.IsAnonymous).HasDefaultValue(false);
            entity.Property(e => e.AuthorDisplayName).HasMaxLength(100);
            entity.Property(e => e.HelpfulCount).HasDefaultValue(0);
            entity.Property(e => e.IsModeratorPick).HasDefaultValue(false);
            
            entity.Property(e => e.TriggerWarnings)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
        });

        // Configure Comment entity
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.IsApproved).HasDefaultValue(false);

            // Self-referencing relationship for nested comments
            entity.HasOne(e => e.ParentComment)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Content
            entity.HasOne(e => e.ContentItem)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ContentId);
            entity.HasIndex(e => e.CreatedBy);
        });

        // Configure ContentInteraction entity
        modelBuilder.Entity<ContentInteraction>(entity =>
        {
            entity.Property(e => e.InteractionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.InteractionType).HasConversion<int>();

            entity.HasOne(e => e.ContentEntity)
                .WithMany(e => e.Interactions)
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite index for user-content interactions
            entity.HasIndex(e => new { e.UserId, e.ContentId, e.InteractionType });
        });

        // Configure ContentRating entity
        modelBuilder.Entity<ContentRating>(entity =>
        {
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Review).HasMaxLength(500);

            entity.HasOne(e => e.ContentEntity)
                .WithMany(e => e.Ratings)
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure one rating per user per content
            entity.HasIndex(e => new { e.UserId, e.ContentId }).IsUnique();
        });

        // Configure Comment entity
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.IsApproved).HasDefaultValue(false);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.ReplyCount).HasDefaultValue(0);

            entity.HasOne(e => e.ContentItem)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentComment)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.ContentId);
            entity.HasIndex(e => e.ParentCommentId);
            entity.HasIndex(e => new { e.ContentId, e.IsApproved, e.ParentCommentId });
        });

        // Configure CommentLike entity
        modelBuilder.Entity<CommentLike>(entity =>
        {
            entity.HasOne(e => e.Comment)
                .WithMany(e => e.Likes)
                .HasForeignKey(e => e.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure one like per user per comment
            entity.HasIndex(e => new { e.UserId, e.CommentId }).IsUnique();
        });

        // Configure OutboxEvent
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.ProcessedAt);
            entity.HasIndex(x => x.NextRetryAt);
            entity.HasIndex(x => new { x.ProcessedAt, x.NextRetryAt, x.RetryCount });
            entity.Property(x => x.EventData).HasColumnType("text");
        });
        
        // Configure CreatorSettings entity  
        modelBuilder.Entity<CreatorSettings>(entity =>
        {
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxContentQuota).HasDefaultValue(50);
            entity.Property(e => e.AutoPublish).HasDefaultValue(false);
            
            entity.HasIndex(e => e.CreatorId).IsUnique();
        });
    }
}