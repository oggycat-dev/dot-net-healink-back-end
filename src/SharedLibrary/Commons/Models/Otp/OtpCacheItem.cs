using SharedLibrary.Commons.Enums;

namespace SharedLibrary.Commons.Models.Otp;

public class OtpCacheItem
{
    public string Contact { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public NotificationChannelEnum Channel { get; set; } = NotificationChannelEnum.Email;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public bool IsVerified { get; set; } = false;
    public OtpTypeEnum Type { get; set; } = OtpTypeEnum.Registration;
    public object userData { get; set; } = string.Empty;
}