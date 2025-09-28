using SharedLibrary.Commons.Entities;
using ContentService.Domain.Enums;

namespace ContentService.Domain.Entities;

public class ContentInteraction : BaseEntity
{
    public Guid ContentId { get; set; }
    public Guid UserId { get; set; }
    public InteractionType InteractionType { get; set; }
    public DateTime InteractionDate { get; set; } = DateTime.UtcNow;
    public string? AdditionalData { get; set; } // JSON for extra data
    
    // Navigation properties
    public virtual Content ContentEntity { get; set; } = null!;
}

public class ContentRating : BaseEntity
{
    public Guid ContentId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string? Review { get; set; }
    
    // Navigation properties
    public virtual Content ContentEntity { get; set; } = null!;
}