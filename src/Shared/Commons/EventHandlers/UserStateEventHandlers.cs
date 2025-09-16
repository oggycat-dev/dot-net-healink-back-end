using Microsoft.Extensions.Logging;
using ProductAuthMicroservice.Commons.Cache;
using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Shared.Contracts.Events;

namespace ProductAuthMicroservice.Commons.EventHandlers;

/// <summary>
/// Event handler cho User Login Event
/// </summary>
public class UserLoggedInEventHandler : IIntegrationEventHandler<UserLoggedInEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserLoggedInEventHandler> _logger;

    public UserLoggedInEventHandler(
        IUserStateCache userStateCache,
        ILogger<UserLoggedInEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserLoggedInEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Update user state in cache khi user login
            var userState = new UserStateInfo
            {
                UserId = @event.UserId,
                Email = @event.Email,
                Roles = @event.Roles,
                Status = ProductAuthMicroservice.Commons.Enums.EntityStatusEnum.Active,
                RefreshToken = @event.RefreshToken,
                RefreshTokenExpiryTime = @event.RefreshTokenExpiryTime,
                LastLoginAt = @event.LoginAt
            };

            await _userStateCache.SetUserStateAsync(userState);
            
            _logger.LogInformation("User state cached for user {UserId} after login", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserLoggedInEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// Event handler cho User Logout Event
/// </summary>
public class UserLoggedOutEventHandler : IIntegrationEventHandler<UserLoggedOutEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserLoggedOutEventHandler> _logger;

    public UserLoggedOutEventHandler(
        IUserStateCache userStateCache,
        ILogger<UserLoggedOutEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserLoggedOutEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Remove user state từ cache khi logout
            await _userStateCache.RemoveUserStateAsync(@event.UserId);
            
            _logger.LogInformation("User state removed from cache for user {UserId} after logout", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserLoggedOutEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// Event handler cho User Status Changed Event
/// </summary>
public class UserStatusChangedEventHandler : IIntegrationEventHandler<UserStatusChangedEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserStatusChangedEventHandler> _logger;

    public UserStatusChangedEventHandler(
        IUserStateCache userStateCache,
        ILogger<UserStatusChangedEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserStatusChangedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Update user status trong cache
            await _userStateCache.UpdateUserStatusAsync(@event.UserId, @event.NewStatus);
            
            // Nếu user bị inactive thì remove khỏi cache
            if (@event.NewStatus != ProductAuthMicroservice.Commons.Enums.EntityStatusEnum.Active)
            {
                await _userStateCache.RemoveUserStateAsync(@event.UserId);
                _logger.LogInformation("User {UserId} removed from cache due to status change to {Status}", 
                    @event.UserId, @event.NewStatus);
            }
            else
            {
                _logger.LogInformation("User {UserId} status updated in cache to {Status}", 
                    @event.UserId, @event.NewStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserStatusChangedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// Event handler cho User Roles Changed Event
/// </summary>
public class UserRolesChangedEventHandler : IIntegrationEventHandler<UserRolesChangedEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserRolesChangedEventHandler> _logger;

    public UserRolesChangedEventHandler(
        IUserStateCache userStateCache,
        ILogger<UserRolesChangedEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserRolesChangedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Update user roles trong cache
            await _userStateCache.UpdateUserRolesAsync(@event.UserId, @event.NewRoles);
            
            _logger.LogInformation("User {UserId} roles updated in cache. New roles: {Roles}", 
                @event.UserId, string.Join(", ", @event.NewRoles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserRolesChangedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// Event handler cho Refresh Token Revoked Event
/// </summary>
public class RefreshTokenRevokedEventHandler : IIntegrationEventHandler<RefreshTokenRevokedEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<RefreshTokenRevokedEventHandler> _logger;

    public RefreshTokenRevokedEventHandler(
        IUserStateCache userStateCache,
        ILogger<RefreshTokenRevokedEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(RefreshTokenRevokedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Revoke refresh token trong cache
            await _userStateCache.RevokeRefreshTokenAsync(@event.UserId);
            
            _logger.LogInformation("Refresh token revoked in cache for user {UserId}. Reason: {Reason}", 
                @event.UserId, @event.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling RefreshTokenRevokedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// Event handler cho User Sessions Invalidated Event
/// </summary>
public class UserSessionsInvalidatedEventHandler : IIntegrationEventHandler<UserSessionsInvalidatedEvent>
{
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<UserSessionsInvalidatedEventHandler> _logger;

    public UserSessionsInvalidatedEventHandler(
        IUserStateCache userStateCache,
        ILogger<UserSessionsInvalidatedEventHandler> logger)
    {
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserSessionsInvalidatedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            // Remove tất cả sessions của user (force logout)
            await _userStateCache.RemoveUserStateAsync(@event.UserId);
            
            _logger.LogInformation("All sessions invalidated for user {UserId}. Reason: {Reason}", 
                @event.UserId, @event.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserSessionsInvalidatedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}
