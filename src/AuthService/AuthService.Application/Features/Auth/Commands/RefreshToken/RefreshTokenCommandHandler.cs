using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;
using SharedLibrary.SharedLibrary.Contracts.Events;

namespace AuthService.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthJwtService _jwtService;
    private readonly IIdentityService _identityService;
    private readonly IUserStateCache _userStateCache;

    public RefreshTokenCommandHandler(
        IOutboxUnitOfWork outboxUnitOfWork, 
        ILogger<RefreshTokenCommandHandler> logger,
        ICurrentUserService currentUserService, 
        IAuthJwtService jwtService, 
        IIdentityService identityService,
        IUserStateCache userStateCache)
    {
        _outboxUnitOfWork = outboxUnitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
        _jwtService = jwtService;
        _identityService = identityService;
        _userStateCache = userStateCache;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
                return Result<AuthResponse>.Failure("Not authorized", ErrorCodeEnum.Unauthorized);
            var user = await _identityService.GetUserByIdAsync(currentUserId);
            if (user == null)
                return Result<AuthResponse>.Failure("User not found", ErrorCodeEnum.NotFound);
            //no need to check refresh token because it is already checked in the distributed redis cache 
            var (accessToken, roles, expiresInMinutes, expiresAt) = _jwtService.GenerateJwtTokenWithExpiration(user);
            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                Roles = roles
            };
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdateEntity(Guid.Parse(currentUserId));
            await _identityService.UpdateUserAsync(user);
            
            // ✅ Get existing cache to preserve UserProfileId and Subscription
            var existingCache = await _userStateCache.GetUserStateAsync(user.Id);
            Guid userProfileId = existingCache?.UserProfileId ?? Guid.Empty;
            
            if (existingCache != null)
            {
                _logger.LogInformation(
                    "UserProfileId retrieved from cache: UserId={UserId}, UserProfileId={UserProfileId}",
                    user.Id, userProfileId);
            }
            else
            {
                _logger.LogWarning(
                    "User cache not found for UserId={UserId} during refresh. UserProfileId will be empty.",
                    user.Id);
            }
            
            // ✅ Update cache with new refresh token (preserve UserProfileId and Subscription)
            var userState = new UserStateInfo
            {
                UserId = user.Id,
                UserProfileId = userProfileId,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Status = user.Status,
                RefreshToken = user.RefreshToken!,  // ✅ NEW refresh token
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime!.Value,
                LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                Subscription = existingCache?.Subscription  // ✅ PRESERVE subscription
            };
            
            _logger.LogInformation(
                "Updating user state in cache after refresh: UserId={UserId}, HasSubscription={HasSubscription}",
                userState.UserId, userState.Subscription != null);
            
            await _userStateCache.SetUserStateAsync(userState);
            
            // ✅ Publish event for activity logging (event consumer will NOT write cache)
            var loginEvent = new UserLoggedInEvent
            {
                UserId = user.Id,
                UserProfileId = userProfileId,
                Email = user.Email!,
                Roles = roles,
                RefreshToken = user.RefreshToken!,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime.Value,
                LoginAt = user.LastLoginAt.Value
            };
            await _outboxUnitOfWork.AddOutboxEventAsync(loginEvent);
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            return Result<AuthResponse>.Success(authResponse, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token: {ErrorMessage}", ex.Message);
            return Result<AuthResponse>.Failure("An error occurred while refreshing token",
             ErrorCodeEnum.InternalError);
        }
    }
}