using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Cache;
using SharedLibrary.SharedLibrary.Contracts.Events;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Contracts.User.Requests;
using SharedLibrary.Contracts.User.Responses;
using SharedLibrary.Contracts.Subscription.Requests;


namespace AuthService.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IUserStateCache _userStateCache;
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IRequestClient<GetUserProfileByUserIdRequest> _userProfileClient;
    private readonly IRequestClient<GetUserSubscriptionRequest> _userSubscriptionClient;

    public LoginCommandHandler(
        IIdentityService identityService, 
        IAuthJwtService jwtService, 
        ILogger<LoginCommandHandler> logger,
        IUserStateCache userStateCache,
        IOutboxUnitOfWork unitOfWork,
        IRequestClient<GetUserProfileByUserIdRequest> userProfileClient,
        IRequestClient<GetUserSubscriptionRequest> userSubscriptionClient)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _logger = logger;
        _userStateCache = userStateCache;
        _unitOfWork = unitOfWork;
        _userProfileClient = userProfileClient;
        _userSubscriptionClient = userSubscriptionClient;
    }
    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _identityService.AuthenticateAsync(command.Request);
            if (!result.IsSuccess)
            {
                return Result<AuthResponse>.Failure(result.Message?? "Invalid credentials", ErrorCodeEnum.InvalidCredentials);
            }

            var user = result.Data!;
            
            //generate refresh token and update auth infor of user
            var (refreshToken, refreshTokenExpiryTime) = _jwtService.GenerateRefreshTokenWithExpiration();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdateEntity(user.Id);
            await _identityService.UpdateUserAsync(user);
            
            //generate jwt token
            var (token, roles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user);
            
            // ✅ Get existing cache first (cache-first pattern)
            var existingCache = await _userStateCache.GetUserStateAsync(user.Id);
            
            // ✅ Try to get UserProfileId from existing cache first, then RPC if needed
            Guid userProfileId = existingCache?.UserProfileId ?? Guid.Empty;
            
            if (userProfileId == Guid.Empty)
            {
                // Only call RPC if not in cache
                try
                {
                    var userProfileRequest = new GetUserProfileByUserIdRequest { UserId = user.Id };
                    var userProfileResponse = await _userProfileClient.GetResponse<GetUserProfileByUserIdResponse>(
                        userProfileRequest, 
                        cancellationToken,
                        RequestTimeout.After(s: 10));

                    if (userProfileResponse.Message.Found)
                    {
                        userProfileId = userProfileResponse.Message.UserProfileId;
                        _logger.LogInformation(
                            "UserProfileId resolved via RPC: UserId={UserId}, UserProfileId={UserProfileId}",
                            user.Id, userProfileId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "UserProfile not found via RPC for UserId={UserId}. UserProfileId will be empty.",
                            user.Id);
                    }
                }
                catch (RequestTimeoutException)
                {
                    _logger.LogError(
                        "Timeout querying UserProfile for UserId={UserId}. UserProfileId will be empty.",
                        user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error querying UserProfile for UserId={UserId}. UserProfileId will be empty.",
                        user.Id);
                }
            }
            else
            {
                _logger.LogInformation(
                    "UserProfileId retrieved from cache: UserId={UserId}, UserProfileId={UserProfileId}",
                    user.Id, userProfileId);
            }
            
            // ✅ Try to load subscription from Subscription Service if not in cache
            var subscriptionInfo = existingCache?.Subscription;
            if (subscriptionInfo == null && userProfileId != Guid.Empty)
            {
                _logger.LogInformation("🔍 DEBUG: Subscription not in cache, attempting to load from Subscription Service for UserProfileId={UserProfileId}", userProfileId);
                
                try
                {
                    var subscriptionRequest = new GetUserSubscriptionRequest { UserProfileId = userProfileId };
                    var subscriptionResponse = await _userSubscriptionClient.GetResponse<GetUserSubscriptionResponse>(
                        subscriptionRequest, 
                        cancellationToken,
                        RequestTimeout.After(s: 10));

                    if (subscriptionResponse.Message.Found)
                    {
                        subscriptionInfo = new UserSubscriptionInfo
                        {
                            SubscriptionId = subscriptionResponse.Message.SubscriptionId!.Value,
                            SubscriptionPlanId = subscriptionResponse.Message.SubscriptionPlanId!.Value,
                            SubscriptionPlanName = subscriptionResponse.Message.SubscriptionPlanName ?? string.Empty,
                            SubscriptionPlanDisplayName = subscriptionResponse.Message.SubscriptionPlanDisplayName ?? string.Empty,
                            SubscriptionStatus = subscriptionResponse.Message.SubscriptionStatus!.Value,
                            CurrentPeriodStart = subscriptionResponse.Message.CurrentPeriodStart,
                            CurrentPeriodEnd = subscriptionResponse.Message.CurrentPeriodEnd,
                            ActivatedAt = subscriptionResponse.Message.ActivatedAt,
                            CanceledAt = subscriptionResponse.Message.CanceledAt
                        };
                        
                        _logger.LogInformation(
                            "🔍 DEBUG: Subscription loaded via RPC: UserProfileId={UserProfileId}, SubscriptionId={SubscriptionId}, Status={Status}, IsActive={IsActive}",
                            userProfileId, subscriptionInfo.SubscriptionId, subscriptionInfo.SubscriptionStatus, subscriptionInfo.IsActive);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "🔍 DEBUG: No active subscription found via RPC for UserProfileId={UserProfileId}",
                            userProfileId);
                    }
                }
                catch (RequestTimeoutException)
                {
                    _logger.LogError(
                        "🔍 DEBUG: Timeout querying subscription for UserProfileId={UserProfileId}. Subscription will remain NULL.",
                        userProfileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "🔍 DEBUG: Error querying subscription for UserProfileId={UserProfileId}. Subscription will remain NULL.",
                        userProfileId);
                }
            }
            
            // Cache user state for distributed auth (with UserProfileId)
            var userState = new UserStateInfo
            {
                UserId = user.Id,                         // UserId for authentication
                UserProfileId = userProfileId,            // ✅ UserProfileId for business logic
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Status = user.Status,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiryTime,
                LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                Subscription = subscriptionInfo  // ✅ Use loaded subscription or existing cache
            };
            
            // 🔍 DEBUG: Log subscription status during login
            _logger.LogInformation(
                "🔍 DEBUG: Login subscription status - UserId={UserId}, HasExistingCache={HasExistingCache}, Subscription={Subscription}",
                user.Id, 
                existingCache != null,
                existingCache?.Subscription != null ? $"Status={existingCache.Subscription.SubscriptionStatus}, IsActive={existingCache.Subscription.IsActive}" : "NULL");
            
            _logger.LogInformation(
                "Setting user state in cache: UserId={UserId}, UserProfileId={UserProfileId}, HasSubscription={HasSubscription}",
                userState.UserId, userState.UserProfileId, userState.Subscription != null);
            
            await _userStateCache.SetUserStateAsync(userState);
            
            // ✅ Verify cache was set correctly
            var cachedState = await _userStateCache.GetUserStateAsync(user.Id);
            if (cachedState != null)
            {
                _logger.LogInformation(
                    "✅ Cache verification SUCCESS: UserId={UserId}, UserProfileId={UserProfileId}",
                    cachedState.UserId, cachedState.UserProfileId);
            }
            else
            {
                _logger.LogError(
                    "❌ Cache verification FAILED: User state not found in cache after set for UserId={UserId}",
                    user.Id);
            }
            
            // Publish login event (with UserProfileId for activity logging)
            var loginEvent = new UserLoggedInEvent
            {
                UserId = user.Id,
                UserProfileId = userProfileId, // ✅ Include UserProfileId for activity logging & cache
                Email = user.Email ?? string.Empty,
                Roles = roles,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiryTime,
                LoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                UserAgent = command.Request.UserAgent,
                IpAddress = command.Request.IpAddress
            };
            await _unitOfWork.AddOutboxEventAsync(loginEvent);
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            
            var authResponse = new AuthResponse
            {
                AccessToken = token,
                Roles = roles,
                ExpiresAt = expiresAt
            };
            
            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Result<AuthResponse>.Success(authResponse, "Login successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in");
            return Result<AuthResponse>.Failure("An error occurred while logging in", ErrorCodeEnum.InternalError);
        }
    }
}