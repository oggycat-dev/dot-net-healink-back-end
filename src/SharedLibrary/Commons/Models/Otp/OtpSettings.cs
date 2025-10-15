public class OtpSettings
{
    public int Length { get; set; } = 6;
    public int ExpirationMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
    public OtpRateLimitingConfiguration RateLimiting { get; set; } = new();
}

public class OtpRateLimitingConfiguration
{
    public RateLimitSettings Registration { get; set; } = new();
    public RateLimitSettings PasswordReset { get; set; } = new();
}

public class RateLimitSettings
{
    /// <summary>
    /// Time window in minutes to track requests
    /// </summary>
    public int WindowMinutes { get; set; } = 10;
    
    /// <summary>
    /// Maximum requests allowed within the window
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 3;
    
    /// <summary>
    /// Cooldown period in seconds between requests
    /// </summary>
    public int CooldownSeconds { get; set; } = 60;
    
    /// <summary>
    /// Block duration in minutes when limit exceeded
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 15;
}