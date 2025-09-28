using System.ComponentModel.DataAnnotations;

namespace ContentService.API.DTOs;

public class CreatePodcastRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [Range(1, 18000)] // Max 5 hours in seconds
    public int Duration { get; set; }

    public List<string> Tags { get; set; } = new();

    [Required]
    public IFormFile AudioFile { get; set; } = null!;
}

public class UpdatePodcastRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }

    [StringLength(2000, MinimumLength = 10)]
    public string? Description { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? Category { get; set; }

    [Range(1, 18000)] // Max 5 hours in seconds
    public int? Duration { get; set; }

    public List<string>? Tags { get; set; }

    public IFormFile? AudioFile { get; set; }
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