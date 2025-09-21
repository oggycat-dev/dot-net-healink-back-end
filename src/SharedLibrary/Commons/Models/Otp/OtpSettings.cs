public class OtpSettings
{
    public int Length { get; set; } = 6;
    public int ExpirationMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
    public int RetryLimitPerHour { get; set; } = 5;
    public int CooldownMinutes { get; set; } = 1;
    //public OtpRateLimitingConfiguration RateLimiting { get; set; } = new();
}