namespace SharedLibrary.Commons.Models.Otp;

/// <summary>
/// Tracks OTP request history for rate limiting
/// </summary>
public class OtpRequestTracker
{
    /// <summary>
    /// Contact (email/phone) being tracked
    /// </summary>
    public string Contact { get; set; } = string.Empty;
    
    /// <summary>
    /// List of request timestamps within the tracking window
    /// </summary>
    public List<DateTime> RequestTimes { get; set; } = new();
    
    /// <summary>
    /// Last request timestamp for cooldown check
    /// </summary>
    public DateTime LastRequestTime { get; set; }
    
    /// <summary>
    /// If blocked, when the block expires
    /// </summary>
    public DateTime? BlockedUntil { get; set; }
}
