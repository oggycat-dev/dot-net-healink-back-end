namespace PodcastRecommendationService.Application.DTOs;

/// <summary>
/// Response DTO for podcast recommendations
/// </summary>
public class PodcastRecommendationResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<RecommendationItem> Recommendations { get; set; } = new();
    public int TotalFound { get; set; }
    public bool FilteredListened { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Individual recommendation item
/// </summary>
public class RecommendationItem
{
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public decimal PredictedRating { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    
    // Additional fields từ FastAPI service
    public string? Category { get; set; }
    public int? DurationMinutes { get; set; }
    public string? ContentUrl { get; set; }
}

/// <summary>
/// Request DTO for getting recommendations
/// </summary>
public class GetRecommendationsRequest
{
    public string UserId { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public bool IncludeListened { get; set; } = false;
}

/// <summary>
/// Request DTO for batch recommendations
/// </summary>
public class BatchRecommendationsRequest
{
    public List<string> UserIds { get; set; } = new();
    public int Limit { get; set; } = 10;
    public bool IncludeListened { get; set; } = false;
}

/// <summary>
/// Response DTO for batch recommendations
/// </summary>
public class BatchRecommendationsResponse
{
    public List<PodcastRecommendationResponse> Results { get; set; } = new();
    public int TotalUsers { get; set; }
    public int SuccessfulUsers { get; set; }
    public List<string> FailedUsers { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for tracking recommendation interactions
/// </summary>
public class RecommendationInteractionRequest
{
    public string PodcastId { get; set; } = string.Empty;
    public string InteractionType { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public decimal? ActualRating { get; set; }
    public string? Feedback { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for user's listened podcasts
/// </summary>
public class UserListenedPodcastsResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<string> ListenedPodcasts { get; set; } = new();
    public int TotalListened { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Individual listened podcast item
/// </summary>
public class ListenedPodcastItem
{
    public string PodcastId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}

/// <summary>
/// AI Service model information response
/// </summary>
public class ModelInfoResponse
{
    public string ModelType { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string TrainingDataStats { get; set; } = string.Empty;
    public string PerformanceMetrics { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public bool IsHealthy { get; set; }
}

public class ModelSummary
{
    public string? InputShape { get; set; }
    public string? OutputShape { get; set; }
    public int? TotalParams { get; set; }
}

/// <summary>
/// DTO for user data from UserService
/// </summary>
public class UserDataDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Preferences { get; set; } // JSON string of user preferences
}

/// <summary>
/// DTO for podcast data from ContentService
/// </summary>
public class PodcastDataDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; } // JSON array of tags
    public string CreatorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; } // in seconds
    public string? AudioUrl { get; set; }
}

/// <summary>
/// DTO for user rating/interaction data
/// </summary>
public class UserRatingDto
{
    public string UserId { get; set; } = string.Empty;
    public string PodcastId { get; set; } = string.Empty;
    public decimal Rating { get; set; } // 1-5 scale
    public string InteractionType { get; set; } = string.Empty; // "listen", "like", "complete", "skip"
    public DateTime InteractionAt { get; set; }
    public int? ListenDuration { get; set; } // seconds listened
    public bool Completed { get; set; }
}