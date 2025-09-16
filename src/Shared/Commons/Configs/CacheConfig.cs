namespace ProductAuthMicroservice.Commons.Configs;

public class CacheConfig
{
    public const string SectionName = "Cache";
    
    /// <summary>
    /// User state cache expiration time in minutes
    /// </summary>
    public int UserStateCacheMinutes { get; set; } = 30;
    
    /// <summary>
    /// User state sliding expiration in minutes
    /// </summary>
    public int UserStateSlidingMinutes { get; set; } = 15;
    
    /// <summary>
    /// Active users list cache time in hours
    /// </summary>
    public int ActiveUsersListHours { get; set; } = 24;
    
    /// <summary>
    /// Cache cleanup interval in minutes
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 60;
    
    /// <summary>
    /// Maximum cache size (number of entries)
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;
}
