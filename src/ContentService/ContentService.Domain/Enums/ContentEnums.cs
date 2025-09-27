namespace ContentService.Domain.Enums;

public enum ContentType
{
    Podcast = 1,
    Flashcard = 2,
    Postcard = 3,
    LettersToMyself = 4,
    CommunityStory = 5
}

public enum ContentStatus
{
    Draft = 1,
    PendingReview = 2,
    PendingModeration = 3,
    Approved = 4,
    Published = 5,
    Rejected = 6,
    Archived = 7
}

public enum InteractionType
{
    Like = 1,
    Share = 2,
    Favorite = 3,
    View = 4,
    Helpful = 5
}

public enum EmotionCategory
{
    Happiness = 1,
    Sadness = 2,
    Anxiety = 3,
    Anger = 4,
    Fear = 5,
    Love = 6,
    Hope = 7,
    Gratitude = 8,
    Mindfulness = 9,
    SelfCompassion = 10
}

public enum TopicCategory
{
    MentalHealth = 1,
    Relationships = 2,
    SelfCare = 3,
    Mindfulness = 4,
    PersonalGrowth = 5,
    WorkLifeBalance = 6,
    Stress = 7,
    Depression = 8,
    Anxiety = 9,
    Therapy = 10
}

// Removed UserRole enum - use SharedLibrary.Commons.Enums.RoleEnum instead