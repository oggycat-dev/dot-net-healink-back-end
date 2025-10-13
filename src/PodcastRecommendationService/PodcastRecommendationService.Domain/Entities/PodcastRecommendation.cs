using SharedLibrary.Commons.Entities;

namespace PodcastRecommendationService.Domain.Entities;

/// <summary>
/// Domain entity representing a podcast recommendation for a user
/// </summary>
public class PodcastRecommendation : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public decimal PredictedRating { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties for tracking recommendation effectiveness
    public bool? WasViewed { get; set; }
    public bool? WasClicked { get; set; }
    public bool? WasCompleted { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}