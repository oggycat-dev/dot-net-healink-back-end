using SharedLibrary.Commons.Entities;

namespace PodcastRecommendationService.Domain.Entities;

/// <summary>
/// Domain entity for tracking user interactions with recommendations
/// </summary>
public class RecommendationInteraction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PodcastId { get; set; } = string.Empty;
    public string InteractionType { get; set; } = string.Empty; // "view", "click", "like", "skip", "complete"
    public DateTime InteractionAt { get; set; } = DateTime.UtcNow;
    public string? SessionId { get; set; }
    public decimal? ActualRating { get; set; } // If user rates after interaction
    public string? Feedback { get; set; }
}