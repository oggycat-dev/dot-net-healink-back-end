using SharedLibrary.Commons.Entities;

namespace ContentService.Domain.Entities;

public class Podcast : Content
{
    public string AudioUrl { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? TranscriptUrl { get; set; }
    public string? HostName { get; set; }
    public string? GuestName { get; set; }
    public int EpisodeNumber { get; set; }
    public string? SeriesName { get; set; }
    
    // Podcast specific analytics
    public TimeSpan? AverageListenTime { get; set; }
    public double? CompletionRate { get; set; }
    
    public Podcast()
    {
        ContentType = Enums.ContentType.Podcast;
    }
}

public class Flashcard : Content
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public int DifficultyLevel { get; set; } // 1-5
    public string[]? RelatedPodcastIds { get; set; }
    
    public Flashcard()
    {
        ContentType = Enums.ContentType.Flashcard;
    }
}

public class Postcard : Content
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? QuoteText { get; set; }
    public string? QuoteAuthor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? FontStyle { get; set; }
    
    public Postcard()
    {
        ContentType = Enums.ContentType.Postcard;
    }
}

public class LettersToMyself : Content
{
    public string LetterContent { get; set; } = string.Empty;
    public DateTime? ScheduledDeliveryDate { get; set; }
    public bool IsDelivered { get; set; }
    public string Mood { get; set; } = string.Empty;
    public string[]? Intentions { get; set; }
    
    public LettersToMyself()
    {
        ContentType = Enums.ContentType.LettersToMyself;
    }
}

public class CommunityStory : Content
{
    public string StoryContent { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string? AuthorDisplayName { get; set; }
    public int HelpfulCount { get; set; }
    public bool IsModeratorPick { get; set; }
    public string[]? TriggerWarnings { get; set; }
    
    public CommunityStory()
    {
        ContentType = Enums.ContentType.CommunityStory;
    }
}