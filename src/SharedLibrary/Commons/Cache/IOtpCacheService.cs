using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models.Otp;

namespace SharedLibrary.Commons.Cache;

public interface IOtpCacheService
{
    Task<(string OtpCode, int ExpiresInMinutes)> GenerateAndStoreOtpAsync(string contact, OtpTypeEnum type, object userData, NotificationChannelEnum channel = NotificationChannelEnum.Email);
    Task<OtpCacheItem?> GetOtpDataAsync(string contact, OtpTypeEnum type);
    Task<OtpResult> VerifyOtpAsync(string contact, string otpCode, OtpTypeEnum type, NotificationChannelEnum channel = NotificationChannelEnum.Email);
    Task RemoveOtpAsync(string contact, OtpTypeEnum type);

    //Utility methods
    Task<int> GetRemainingAttemptsAsync(string contact, OtpTypeEnum type);
    Task<TimeSpan> GetRemainingTimeAsync(string contact, OtpTypeEnum type);
    
    // Rate Limiting methods
    Task<(bool IsAllowed, string Reason)> CheckRateLimitingAsync(string contact, OtpTypeEnum type);
    Task TrackOtpRequestAsync(string contact, OtpTypeEnum type);
    Task<(bool IsBlocked, TimeSpan? RemainingTime)> GetRateLimitStatusAsync(string contact);
    Task ClearRateLimitTrackerAsync(string contact);
}