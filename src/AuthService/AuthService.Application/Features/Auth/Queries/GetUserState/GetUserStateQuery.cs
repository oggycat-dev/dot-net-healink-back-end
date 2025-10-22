using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Services;

namespace AuthService.Application.Features.Auth.Queries.GetUserState;

/// <summary>
/// Query to get current user state from cache with fallback to identity service
/// </summary>
public record GetUserStateQuery : IRequest<Result<UserStateResponse>>;

/// <summary>
/// Query to check if current user is content creator
/// </summary>
public record IsContentCreatorQuery : IRequest<Result<ContentCreatorStatusResponse>>;

/// <summary>
/// Handler for GetUserStateQuery
/// </summary>
public class GetUserStateQueryHandler : IRequestHandler<GetUserStateQuery, Result<UserStateResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;
    private readonly IIdentityService _identityService;
    private readonly ILogger<GetUserStateQueryHandler> _logger;

    public GetUserStateQueryHandler(
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache,
        IIdentityService identityService,
        ILogger<GetUserStateQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<Result<UserStateResponse>> Handle(GetUserStateQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from JWT
            var (isValid, userId) = await _currentUserService.IsUserValidAsync();
            if (!isValid || !userId.HasValue)
            {
                _logger.LogWarning("Invalid user or user not found");
                return Result<UserStateResponse>.Failure("User not found or inactive", ErrorCodeEnum.Unauthorized);
            }

            // Try to get user state from cache first
            var userState = await _userStateCache.GetUserStateAsync(userId.Value);
            
            if (userState != null)
            {
                _logger.LogDebug("User state loaded from cache for user {UserId}", userId.Value);
                return Result<UserStateResponse>.Success(MapToResponse(userState, "cache"));
            }

            // Fallback to identity service
            _logger.LogDebug("User state not found in cache, falling back to identity service for user {UserId}", userId.Value);
            return await GetUserStateFromIdentityService(userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user state");
            return Result<UserStateResponse>.Failure("Internal server error", ErrorCodeEnum.InternalError);
        }
    }

    private async Task<Result<UserStateResponse>> GetUserStateFromIdentityService(Guid userId)
    {
        try
        {
            // Get user from identity service
            var user = await _identityService.GetUserByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found in identity service: {UserId}", userId);
                return Result<UserStateResponse>.Failure("User not found", ErrorCodeEnum.NotFound);
            }

            // Get user roles
            var rolesResult = await _identityService.GetUserRolesAsync(user);
            if (!rolesResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get user roles: {UserId}", userId);
                return Result<UserStateResponse>.Failure("Failed to get user roles", ErrorCodeEnum.InternalError);
            }

            // Create response from identity service data
            var response = new UserStateResponse
            {
                UserId = userId,
                UserProfileId = userId, // Assuming same for now
                Email = user.Email ?? string.Empty,
                Roles = rolesResult.Data?.ToList() ?? new List<string>(),
                Status = user.Status.ToString(),
                LastLoginAt = DateTime.UtcNow, // Approximate
                CacheUpdatedAt = DateTime.UtcNow,
                Subscription = null, // Not available from identity service
                IsActive = user.Status == EntityStatusEnum.Active,
                HasActiveSubscription = false, // Not available from identity service
                IsContentCreator = rolesResult.Data?.Contains("ContentCreator", StringComparer.OrdinalIgnoreCase) ?? false,
            };

            _logger.LogDebug("User state loaded from identity service for user {UserId}", userId);
            return Result<UserStateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user state from identity service: {UserId}", userId);
            return Result<UserStateResponse>.Failure("Failed to get user state from identity service", ErrorCodeEnum.InternalError);
        }
    }

    private static UserStateResponse MapToResponse(SharedLibrary.Commons.Cache.UserStateInfo userState, string source)
    {
        return new UserStateResponse
        {
            UserId = userState.UserId,
            UserProfileId = userState.UserProfileId,
            Email = userState.Email,
            Roles = userState.Roles.ToList(),
            Status = userState.Status.ToString(),
            LastLoginAt = userState.LastLoginAt,
            CacheUpdatedAt = userState.CacheUpdatedAt,
            Subscription = userState.Subscription != null ? new UserSubscriptionResponse
            {
                SubscriptionId = userState.Subscription.SubscriptionId,
                SubscriptionPlanId = userState.Subscription.SubscriptionPlanId,
                SubscriptionPlanName = userState.Subscription.SubscriptionPlanName,
                SubscriptionPlanDisplayName = userState.Subscription.SubscriptionPlanDisplayName,
                SubscriptionStatus = userState.Subscription.SubscriptionStatus,
                SubscriptionStatusName = GetSubscriptionStatusName(userState.Subscription.SubscriptionStatus),
                CurrentPeriodStart = userState.Subscription.CurrentPeriodStart,
                CurrentPeriodEnd = userState.Subscription.CurrentPeriodEnd,
                ActivatedAt = userState.Subscription.ActivatedAt,
                CanceledAt = userState.Subscription.CanceledAt,
                IsActive = userState.Subscription.IsActive,
                IsExpired = userState.Subscription.IsExpired,
            } : null,
            IsActive = userState.IsActive,
            HasActiveSubscription = userState.HasActiveSubscription,
            IsContentCreator = userState.Roles.Contains("ContentCreator", StringComparer.OrdinalIgnoreCase),
        };
    }

    private static string GetSubscriptionStatusName(int status)
    {
        return status switch
        {
            0 => "Pending",
            1 => "Active",
            2 => "Expired",
            3 => "Canceled",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Handler for IsContentCreatorQuery
/// </summary>
public class IsContentCreatorQueryHandler : IRequestHandler<IsContentCreatorQuery, Result<ContentCreatorStatusResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserStateCache _userStateCache;
    private readonly IIdentityService _identityService;
    private readonly ILogger<IsContentCreatorQueryHandler> _logger;

    public IsContentCreatorQueryHandler(
        ICurrentUserService currentUserService,
        IUserStateCache userStateCache,
        IIdentityService identityService,
        ILogger<IsContentCreatorQueryHandler> logger)
    {
        _currentUserService = currentUserService;
        _userStateCache = userStateCache;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<Result<ContentCreatorStatusResponse>> Handle(IsContentCreatorQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from JWT
            var (isValid, userId) = await _currentUserService.IsUserValidAsync();
            if (!isValid || !userId.HasValue)
            {
                _logger.LogWarning("Invalid user or user not found");
                return Result<ContentCreatorStatusResponse>.Failure("User not found or inactive", ErrorCodeEnum.Unauthorized);
            }

            // Try to get user state from cache first
            var userState = await _userStateCache.GetUserStateAsync(userId.Value);
            
            if (userState != null)
            {
                var isContentCreator = userState.Roles.Contains("ContentCreator", StringComparer.OrdinalIgnoreCase);
                _logger.LogDebug("Content creator status loaded from cache for user {UserId}: {IsContentCreator}", userId.Value, isContentCreator);
                
                return Result<ContentCreatorStatusResponse>.Success(new ContentCreatorStatusResponse
                {
                    IsContentCreator = isContentCreator,
                    Reason = isContentCreator ? "User has ContentCreator role" : "User does not have ContentCreator role",
                    CheckedAt = DateTime.UtcNow,
                    Source = "cache"
                });
            }

            // Fallback to identity service
            _logger.LogDebug("User state not found in cache, checking content creator status via identity service for user {UserId}", userId.Value);
            return await CheckContentCreatorFromIdentityService(userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking content creator status");
            return Result<ContentCreatorStatusResponse>.Failure("Internal server error", ErrorCodeEnum.InternalError);
        }
    }

    private async Task<Result<ContentCreatorStatusResponse>> CheckContentCreatorFromIdentityService(Guid userId)
    {
        try
        {
            // Get user from identity service
            var user = await _identityService.GetUserByIdAsync(userId.ToString());
            if (user == null || user.Status != EntityStatusEnum.Active || user.RefreshToken != null || user.RefreshTokenExpiryTime >= DateTime.UtcNow)
            {
                _logger.LogWarning("User not found in identity service: {UserId}", userId);
                return Result<ContentCreatorStatusResponse>.Failure("User not found or inactive", ErrorCodeEnum.Unauthorized);
            }

            // Get user roles
            var rolesResult = await _identityService.GetUserRolesAsync(user);
            if (!rolesResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get user roles: {UserId}", userId);
                return Result<ContentCreatorStatusResponse>.Failure("Failed to get user roles", ErrorCodeEnum.InternalError);
            }

            var isContentCreator = rolesResult.Data?.Contains("ContentCreator", StringComparer.OrdinalIgnoreCase) ?? false;
            _logger.LogDebug("Content creator status loaded from identity service for user {UserId}: {IsContentCreator}", userId, isContentCreator);

            return Result<ContentCreatorStatusResponse>.Success(new ContentCreatorStatusResponse
            {
                IsContentCreator = isContentCreator,
                Reason = isContentCreator ? "User has ContentCreator role" : "User does not have ContentCreator role",
                CheckedAt = DateTime.UtcNow,
                Source = "identity_service"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking content creator status from identity service: {UserId}", userId);
            return Result<ContentCreatorStatusResponse>.Failure("Failed to check content creator status", ErrorCodeEnum.InternalError);
        }
    }
}
