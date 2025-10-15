using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models.Otp;

namespace SharedLibrary.Commons.Cache;

public class OtpCacheService : IOtpCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly OtpSettings _otpSettings;
    private readonly ILogger<OtpCacheService> _logger;

    public OtpCacheService(
        IDistributedCache distributedCache, 
        IOptions<OtpSettings> otpSettings,
        ILogger<OtpCacheService> logger)
    {
        _distributedCache = distributedCache;
        _otpSettings = otpSettings.Value;
        _logger = logger;
    }

    //helper method to generate OTP
    private string GenerateSecureOtp(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        var chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            int digit = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 10;
            chars[i] = (char)('0' + digit); // chuyển số thành ký tự trực tiếp
        }

        return new string(chars);
    }

    //helper method to generate cache key
    private string GenerateCacheKey(string contact, OtpTypeEnum type)
    {
        return $"otp_{type.ToString().ToLowerInvariant()}_{contact.ToLowerInvariant()}";
    }

    public async Task<(string OtpCode, int ExpiresInMinutes)> GenerateAndStoreOtpAsync(string contact, OtpTypeEnum type, object userData, NotificationChannelEnum channel = NotificationChannelEnum.Email)
    {
        try
        {
            //3. Generate OTP
            var otpCode = GenerateSecureOtp(_otpSettings.Length);
            //4. Generate Cache Key
            var cacheKey = GenerateCacheKey(contact, type);

            //5. Create OtpCacheItem
            var cacheItem = new OtpCacheItem
            {
                Contact = contact,
                OtpCode = otpCode,
                Channel = channel,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_otpSettings.ExpirationMinutes),
                CreatedAt = DateTime.UtcNow,
                AttemptCount = 0,
                MaxAttempts = _otpSettings.MaxAttempts,
                IsVerified = false,
                Type = type,
                userData = userData
            };
            //6. Store OTP in cache
            var expirationTime = TimeSpan.FromMinutes(_otpSettings.ExpirationMinutes);
            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheItem), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime
            });

            return (otpCode, _otpSettings.ExpirationMinutes);
        }
        catch
        {
            throw;
        }

    }

    public async Task<OtpCacheItem?> GetOtpDataAsync(string contact, OtpTypeEnum type)
    {
        var cacheKey = GenerateCacheKey(contact, type);
        var cachedData = await _distributedCache.GetStringAsync(cacheKey);
        return string.IsNullOrEmpty(cachedData) ? null : JsonSerializer.Deserialize<OtpCacheItem>(cachedData);
    }

    public async Task<OtpResult> VerifyOtpAsync(string contact, string otpCode, OtpTypeEnum type, NotificationChannelEnum channel = NotificationChannelEnum.Email)
    {

        try
        {
            //1. Generate Cache Key
            var cacheKey = GenerateCacheKey(contact, type);
            //2. Try get cache item
            var cacheItem = await GetOtpDataAsync(contact, type);
            //3. If valid cache item found, increase attempt count and update
            if (cacheItem == null)
            {
                return new OtpResult
                {
                    Success = false,
                    Message = "No valid OTP found or OTP has expired."
                };
            }

            if (cacheItem.Channel != channel)
            {
                return new OtpResult
                {
                    Success = false,
                    Message = "OTP channel mismatch."
                };
            }

            if (cacheItem.IsVerified)
            {
                await RemoveOtpAsync(contact, type);
                return new OtpResult
                {
                    Success = false,
                    Message = "OTP has already been verified."
                };
            }

            cacheItem.AttemptCount++;

            // Calculate remaining time for cache expiration
            var remainingTime = cacheItem.ExpiresAt - DateTime.UtcNow;
            if (remainingTime <= TimeSpan.Zero)
            {
                await RemoveOtpAsync(contact, type);
                return new OtpResult
                {
                    Success = false,
                    Message = "OTP has expired."
                };
            }

            _distributedCache.SetString(cacheKey, JsonSerializer.Serialize(cacheItem), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = remainingTime
            });
            //4. check max attempts
            if (cacheItem.AttemptCount > cacheItem.MaxAttempts)
            {
                // Background cleanup when max attempts exceeded
                _ = Task.Run(async () =>
                {
                    await RemoveOtpAsync(contact, type);
                    //ClearRateLimitTracker(contact);
                });

                return new OtpResult
                {
                    Success = false,
                    Message = "Max OTP attempts exceeded."
                };
            }

            //5. If check max attempts passed, verify OTP code
            bool isValid = cacheItem.OtpCode.Equals(otpCode, StringComparison.Ordinal);

            //6. if verified, mark as verified and update cache item (do not remove yet for cqrs use case)

            if (!isValid)
            {
                return new OtpResult
                {
                    Success = false,
                    Message = "Invalid OTP code.",
                    RemainingAttempts = await GetRemainingAttemptsAsync(contact, type),
                    ExpiresIn = await GetRemainingTimeAsync(contact, type)
                };
            }
            cacheItem.IsVerified = true;
            // Background cleanup when max attempts exceeded
            _ = Task.Run(async () =>
            {
                await RemoveOtpAsync(contact, type);
                //ClearRateLimitTracker(contact);
            });
            return new OtpResult
            {
                Success = true,
                Message = "OTP verified successfully.",
                UserData = cacheItem.userData,
            };
        }
        catch
        {
            return new OtpResult
            {
                Success = false,
                Message = "Error verifying OTP."
            };
        }
    }


    public async Task RemoveOtpAsync(string contact, OtpTypeEnum type)
    {
        var cacheKey = GenerateCacheKey(contact, type);
        await _distributedCache.RemoveAsync(cacheKey);
    }

    public async Task<int> GetRemainingAttemptsAsync(string contact, OtpTypeEnum type)
    {
        var cacheItem = await GetOtpDataAsync(contact, type);
        if (cacheItem == null)
            return 0;

        return Math.Max(0, cacheItem.MaxAttempts - cacheItem.AttemptCount);
    }

    public async Task<TimeSpan> GetRemainingTimeAsync(string contact, OtpTypeEnum type)
    {
        var cacheItem = await GetOtpDataAsync(contact, type);
        if (cacheItem == null || cacheItem.ExpiresAt <= DateTime.UtcNow)
            return TimeSpan.Zero;

        return cacheItem.ExpiresAt - DateTime.UtcNow;
    }

    #region Rate Limiting Methods

    /// <summary>
    /// Generate rate limiting tracker key
    /// </summary>
    private string GenerateRateLimitKey(string contact, OtpTypeEnum type)
    {
        return $"otp_rate_limit_{type.ToString().ToLowerInvariant()}_{contact.ToLowerInvariant()}";
    }

    /// <summary>
    /// Check if request is allowed based on rate limiting rules
    /// </summary>
    public async Task<(bool IsAllowed, string Reason)> CheckRateLimitingAsync(string contact, OtpTypeEnum type)
    {
        var now = DateTime.UtcNow;
        var rateLimitKey = GenerateRateLimitKey(contact, type);
        
        // Get rate limiting settings based on OTP type
        var rateLimitSettings = type switch
        {
            OtpTypeEnum.Registration => _otpSettings.RateLimiting.Registration,
            OtpTypeEnum.PasswordReset => _otpSettings.RateLimiting.PasswordReset,
            _ => _otpSettings.RateLimiting.Registration
        };
        
        // Get tracker from Redis
        var trackerJson = await _distributedCache.GetStringAsync(rateLimitKey);
        var tracker = string.IsNullOrEmpty(trackerJson) 
            ? new OtpRequestTracker 
            { 
                Contact = contact,
                RequestTimes = new List<DateTime>(),
                LastRequestTime = DateTime.MinValue
            }
            : JsonSerializer.Deserialize<OtpRequestTracker>(trackerJson)!;

        // Check if currently blocked
        if (tracker.BlockedUntil.HasValue && tracker.BlockedUntil > now)
        {
            var remainingTime = tracker.BlockedUntil.Value - now;
            return (false, $"Too many {type} OTP requests. Please try again in {remainingTime.Minutes} minutes and {remainingTime.Seconds} seconds.");
        }

        // Check cooldown period (in seconds, not minutes!)
        var cooldownSeconds = rateLimitSettings.CooldownSeconds;
        if (tracker.LastRequestTime != DateTime.MinValue && 
            tracker.LastRequestTime.AddSeconds(cooldownSeconds) > now)
        {
            var remainingCooldown = tracker.LastRequestTime.AddSeconds(cooldownSeconds) - now;
            return (false, $"Please wait {Math.Ceiling(remainingCooldown.TotalSeconds)} seconds before requesting another {type} OTP.");
        }

        // Clean up old requests outside the window
        var windowStart = now.AddMinutes(-rateLimitSettings.WindowMinutes);
        tracker.RequestTimes.RemoveAll(rt => rt < windowStart);

        // Check rate limit within window
        if (tracker.RequestTimes.Count >= rateLimitSettings.MaxRequestsPerWindow)
        {
            // Block the contact
            tracker.BlockedUntil = now.AddMinutes(rateLimitSettings.BlockDurationMinutes);
            
            // Save blocked tracker to Redis
            await _distributedCache.SetStringAsync(
                rateLimitKey, 
                JsonSerializer.Serialize(tracker),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(rateLimitSettings.BlockDurationMinutes)
                });
            
            _logger.LogWarning("Rate limit exceeded for {Contact}, type {Type}. Blocked for {Duration} minutes.", 
                contact, type, rateLimitSettings.BlockDurationMinutes);
            
            return (false, $"Too many {type} OTP requests. You are blocked for {rateLimitSettings.BlockDurationMinutes} minutes.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Track OTP request for rate limiting
    /// </summary>
    public async Task TrackOtpRequestAsync(string contact, OtpTypeEnum type)
    {
        var now = DateTime.UtcNow;
        var rateLimitKey = GenerateRateLimitKey(contact, type);
        
        // Get rate limiting settings based on OTP type
        var rateLimitSettings = type switch
        {
            OtpTypeEnum.Registration => _otpSettings.RateLimiting.Registration,
            OtpTypeEnum.PasswordReset => _otpSettings.RateLimiting.PasswordReset,
            _ => _otpSettings.RateLimiting.Registration
        };
        
        // Get or create tracker
        var trackerJson = await _distributedCache.GetStringAsync(rateLimitKey);
        var tracker = string.IsNullOrEmpty(trackerJson) 
            ? new OtpRequestTracker 
            { 
                Contact = contact,
                RequestTimes = new List<DateTime>(),
                LastRequestTime = DateTime.MinValue
            }
            : JsonSerializer.Deserialize<OtpRequestTracker>(trackerJson)!;

        // Add current request
        tracker.RequestTimes.Add(now);
        tracker.LastRequestTime = now;
        
        // Clean up old requests
        var windowStart = now.AddMinutes(-rateLimitSettings.WindowMinutes);
        tracker.RequestTimes.RemoveAll(rt => rt < windowStart);

        // Save tracker to Redis with expiration
        await _distributedCache.SetStringAsync(
            rateLimitKey, 
            JsonSerializer.Serialize(tracker),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(rateLimitSettings.WindowMinutes + rateLimitSettings.BlockDurationMinutes)
            });

        _logger.LogInformation("Tracked OTP request for {Contact}, type {Type}. Total requests in window: {Count}", 
            contact, type, tracker.RequestTimes.Count);
    }

    /// <summary>
    /// Get rate limit status for a contact
    /// </summary>
    public async Task<(bool IsBlocked, TimeSpan? RemainingTime)> GetRateLimitStatusAsync(string contact)
    {
        var now = DateTime.UtcNow;
        
        // Check both Registration and PasswordReset trackers
        foreach (var otpType in new[] { OtpTypeEnum.Registration, OtpTypeEnum.PasswordReset })
        {
            var rateLimitKey = GenerateRateLimitKey(contact, otpType);
            var trackerJson = await _distributedCache.GetStringAsync(rateLimitKey);
            
            if (!string.IsNullOrEmpty(trackerJson))
            {
                var tracker = JsonSerializer.Deserialize<OtpRequestTracker>(trackerJson)!;
                
                // Check if blocked
                if (tracker.BlockedUntil.HasValue && tracker.BlockedUntil > now)
                {
                    return (true, tracker.BlockedUntil.Value - now);
                }
            }
        }
        
        return (false, null);
    }

    /// <summary>
    /// Clear rate limiting tracker for a contact
    /// </summary>
    public async Task ClearRateLimitTrackerAsync(string contact)
    {
        // Clear all rate limiting trackers for this contact (both Registration and PasswordReset)
        var registrationKey = GenerateRateLimitKey(contact, OtpTypeEnum.Registration);
        var passwordResetKey = GenerateRateLimitKey(contact, OtpTypeEnum.PasswordReset);
        
        await _distributedCache.RemoveAsync(registrationKey);
        await _distributedCache.RemoveAsync(passwordResetKey);
        
        _logger.LogInformation("Cleared rate limiting trackers for contact {Contact}", contact);
    }

    #endregion
}
