public class OtpSettings
{
    public int Length { get; set; }
    public int ExpirationMinutes { get; set; }
    public int MaxAttempts { get; set; }
    //public OtpRateLimitingConfiguration RateLimiting { get; set; } = new();
}