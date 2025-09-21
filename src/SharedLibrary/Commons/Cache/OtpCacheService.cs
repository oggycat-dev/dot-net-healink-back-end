using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models.Otp;

namespace SharedLibrary.Commons.Cache;

public class OtpCacheService : IOtpCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly OtpSettings _otpSettings;

    public OtpCacheService(IDistributedCache distributedCache, IOptions<OtpSettings> otpSettings)
    {
        _distributedCache = distributedCache;
        _otpSettings = otpSettings.Value;
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
}