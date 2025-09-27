using ContentService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ContentService.API.DTOs;

public class CreatePodcastRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public IFormFile AudioFile { get; set; } = null!;

    [Required]
    public TimeSpan Duration { get; set; }

    public string? TranscriptUrl { get; set; }

    [StringLength(100)]
    public string? HostName { get; set; }

    [StringLength(100)]
    public string? GuestName { get; set; }

    [Range(1, int.MaxValue)]
    public int EpisodeNumber { get; set; }

    [StringLength(100)]
    public string? SeriesName { get; set; }

    public string[]? Tags { get; set; }

    public EmotionCategory[]? EmotionCategories { get; set; }

    public TopicCategory[]? TopicCategories { get; set; }

    public IFormFile? ThumbnailFile { get; set; }
}

public class UpdatePodcastRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    public string? TranscriptUrl { get; set; }

    [StringLength(100)]
    public string? HostName { get; set; }

    [StringLength(100)]
    public string? GuestName { get; set; }

    [StringLength(100)]
    public string? SeriesName { get; set; }

    public string[]? Tags { get; set; }

    public EmotionCategory[]? EmotionCategories { get; set; }

    public TopicCategory[]? TopicCategories { get; set; }
}

public class PodcastResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? TranscriptUrl { get; set; }
    public string? HostName { get; set; }
    public string? GuestName { get; set; }
    public int EpisodeNumber { get; set; }
    public string? SeriesName { get; set; }
    public string[]? Tags { get; set; }
    public EmotionCategory[]? EmotionCategories { get; set; }
    public TopicCategory[]? TopicCategories { get; set; }
    public ContentStatus ContentStatus { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class ApprovePodcastRequestDto
{
    public bool IsApproved { get; set; }
    public string? Notes { get; set; }
}