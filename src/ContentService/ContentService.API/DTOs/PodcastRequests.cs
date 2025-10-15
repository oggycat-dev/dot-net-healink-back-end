using System.ComponentModel.DataAnnotations;
using ContentService.Domain.Enums;

namespace ContentService.API.DTOs;

public class CreatePodcastRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 18000)] // Max 5 hours in seconds
    public int Duration { get; set; }

    // Podcast specific fields
    [StringLength(1000)]
    public string? TranscriptUrl { get; set; }

    [StringLength(100)]
    public string? HostName { get; set; }

    [StringLength(100)]
    public string? GuestName { get; set; }

    [Range(1, 10000)]
    public int EpisodeNumber { get; set; } = 1;

    [StringLength(200)]
    public string? SeriesName { get; set; }

    // Categories and tags
    public List<string> Tags { get; set; } = new();

    public List<EmotionCategory> EmotionCategories { get; set; } = new();

    public List<TopicCategory> TopicCategories { get; set; } = new();

    // Files
    [Required]
    public IFormFile AudioFile { get; set; } = null!;

    public IFormFile? ThumbnailFile { get; set; }
}

public class UpdatePodcastRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }

    [StringLength(2000, MinimumLength = 10)]
    public string? Description { get; set; }

    [Range(1, 18000)] // Max 5 hours in seconds
    public int? Duration { get; set; }

    // Podcast specific fields
    [StringLength(1000)]
    public string? TranscriptUrl { get; set; }

    [StringLength(100)]
    public string? HostName { get; set; }

    [StringLength(100)]
    public string? GuestName { get; set; }

    [Range(1, 10000)]
    public int? EpisodeNumber { get; set; }

    [StringLength(200)]
    public string? SeriesName { get; set; }

    // Categories and tags
    public List<string>? Tags { get; set; }

    public List<EmotionCategory>? EmotionCategories { get; set; }

    public List<TopicCategory>? TopicCategories { get; set; }

    // Files
    public IFormFile? AudioFile { get; set; }

    public IFormFile? ThumbnailFile { get; set; }
}

public class ApprovePodcastRequest
{
    [StringLength(500)]
    public string? ApprovalNotes { get; set; }
}

public class RejectPodcastRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string RejectionReason { get; set; } = string.Empty;
}