using SharedLibrary.Commons.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class Content : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public ContentType ContentType { get; set; }
    public ContentStatus ContentStatus { get; set; } = ContentStatus.Draft;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // SEO & Discovery
    public string[]? Tags { get; set; }
    public EmotionCategory[]? EmotionCategories { get; set; }
    public TopicCategory[]? TopicCategories { get; set; }
    
    // Analytics
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int ShareCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public double? AverageRating { get; set; }
    
    // Relationships
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<ContentInteraction> Interactions { get; set; } = new List<ContentInteraction>();
    public virtual ICollection<ContentRating> Ratings { get; set; } = new List<ContentRating>();
    
    // Polymorphic properties - specific content data stored as JSON
    public string? ContentData { get; set; } // JSON storage for type-specific data
}