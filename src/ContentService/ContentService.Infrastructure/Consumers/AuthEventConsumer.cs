using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.SharedLibrary.Contracts.Events;
using ContentService.Infrastructure.Context;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;

namespace ContentService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý authentication và authorization events từ Auth Service
/// </summary>
public class AuthEventConsumer : 
    IConsumer<UserLoggedInEvent>,
    IConsumer<UserLoggedOutEvent>,
    IConsumer<UserStatusChangedEvent>,
    IConsumer<UserRolesChangedEvent>,
    IConsumer<UserLockStatusChangedEvent>,
    IConsumer<RoleAddedToUserEvent>,
    IConsumer<RoleRemovedFromUserEvent>,
    IConsumer<RefreshTokenRevokedEvent>,
    IConsumer<UserSessionsInvalidatedEvent>
{
    private readonly ILogger<AuthEventConsumer> _logger;
    private readonly ContentDbContext _context;
    private readonly IUserStateCache _userCache;

    public AuthEventConsumer(
        ILogger<AuthEventConsumer> logger, 
        ContentDbContext context,
        IUserStateCache userCache)
    {
        _logger = logger;
        _context = context;
        _userCache = userCache;
    }

    public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        var loginEvent = context.Message;
        _logger.LogInformation("Processing UserLoggedInEvent for User: {UserId} - {Email} at {LoginTime}", 
            loginEvent.UserId, loginEvent.Email, loginEvent.LoginAt);

        try
        {
            // ✅ Cache is already set by AuthService's LoginCommandHandler
            // No need to write cache here (avoid double write)
            
            // ✅ Verify cache exists (for monitoring)
            var cachedState = await _userCache.GetUserStateAsync(loginEvent.UserId);
            if (cachedState != null)
            {
                _logger.LogInformation(
                    "User state verified in cache: UserId={UserId}, UserProfileId={UserProfileId}, HasSubscription={HasSubscription}",
                    cachedState.UserId, cachedState.UserProfileId, cachedState.Subscription != null);
            }
            else
            {
                _logger.LogWarning(
                    "User state NOT found in cache after login for UserId={UserId}. This may indicate a cache sync issue.",
                    loginEvent.UserId);
            }

            // TODO: Pre-load user's favorite content types, recent content, etc.
            // Could track login patterns for content recommendation algorithms
            // Could initialize content preferences, load recommended content, etc.

            _logger.LogDebug("User login event processed for content personalization: {UserId}", loginEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserLoggedInEvent for UserId: {UserId}", loginEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserLoggedOutEvent> context)
    {
        var logoutEvent = context.Message;
        _logger.LogInformation("Processing UserLoggedOutEvent for User: {UserId} - {Email} - Type: {LogoutType}", 
            logoutEvent.UserId, logoutEvent.Email, logoutEvent.LogoutType);

        try
        {
            // Clear any user-specific content caches
            await _userCache.RemoveUserStateAsync(logoutEvent.UserId);
            
            // End any active content sessions (like podcast listening sessions)
            // TODO: Save content progress, pause ongoing content consumption, etc.
            
            _logger.LogDebug("Cleared content session data for logged out user: {UserId}", logoutEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserLoggedOutEvent for UserId: {UserId}", logoutEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserStatusChangedEvent> context)
    {
        var statusEvent = context.Message;
        _logger.LogInformation("Processing UserStatusChangedEvent for User: {UserId} - {OldStatus} → {NewStatus}", 
            statusEvent.UserId, statusEvent.OldStatus, statusEvent.NewStatus);

        try
        {
            // Handle user status changes affecting content visibility
            var userContents = await _context.Contents
                .Where(c => c.CreatedBy == statusEvent.UserId)
                .ToListAsync();

            foreach (var content in userContents)
            {
                if (statusEvent.NewStatus == SharedLibrary.Commons.Enums.EntityStatusEnum.Inactive)
                {
                    // Hide content from inactive/suspended users
                    if (content.ContentStatus == ContentService.Domain.Enums.ContentStatus.Published)
                    {
                        content.ContentStatus = ContentService.Domain.Enums.ContentStatus.Archived;
                        content.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            if (userContents.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated content visibility for {ContentCount} contents due to user status change to {NewStatus}", 
                    userContents.Count, statusEvent.NewStatus);
            }

            // Update user cache
            await _userCache.UpdateUserStatusAsync(statusEvent.UserId, statusEvent.NewStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserStatusChangedEvent for UserId: {UserId}", statusEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserRolesChangedEvent> context)
    {
        var roleEvent = context.Message;
        _logger.LogInformation("Processing UserRolesChangedEvent for User: {UserId} - Added: [{AddedRoles}], Removed: [{RemovedRoles}]", 
            roleEvent.UserId, 
            string.Join(", ", roleEvent.AddedRoles), 
            string.Join(", ", roleEvent.RemovedRoles));

        try
        {
            // Update content permissions based on role changes
            if (roleEvent.NewRoles?.Any() == true)
            {
                await _userCache.UpdateUserRolesAsync(roleEvent.UserId, roleEvent.NewRoles.ToList());
            }

            // Handle role-specific content permissions
            if (roleEvent.AddedRoles.Contains("Moderator") || roleEvent.AddedRoles.Contains("Admin"))
            {
                _logger.LogInformation("User {UserId} granted moderation roles, enabling content moderation features", 
                    roleEvent.UserId);
                
                // TODO: Enable content moderation dashboard, content approval queue access, etc.
            }

            if (roleEvent.RemovedRoles.Contains("Moderator") || roleEvent.RemovedRoles.Contains("Admin"))
            {
                _logger.LogInformation("User {UserId} removed from moderation roles, disabling content moderation features", 
                    roleEvent.UserId);
                
                // TODO: Revoke content moderation permissions
            }

            // Handle content creation permissions based on roles
            if (roleEvent.AddedRoles.Contains("PremiumUser"))
            {
                // TODO: Enable premium content features, higher upload limits, etc.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserRolesChangedEvent for UserId: {UserId}", roleEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserLockStatusChangedEvent> context)
    {
        var lockEvent = context.Message;
        _logger.LogWarning("Processing UserLockStatusChangedEvent for User: {UserId} - Locked: {IsLocked} - Reason: {LockReason}", 
            lockEvent.UserId, lockEvent.IsLocked, lockEvent.LockReason);

        try
        {
            if (lockEvent.IsLocked)
            {
                // Hide/archive content from locked users
                var userContents = await _context.Contents
                    .Where(c => c.CreatedBy == lockEvent.UserId && 
                               c.ContentStatus == ContentService.Domain.Enums.ContentStatus.Published)
                    .ToListAsync();

                foreach (var content in userContents)
                {
                    content.ContentStatus = ContentService.Domain.Enums.ContentStatus.Archived;
                    content.UpdatedAt = DateTime.UtcNow;
                }

                if (userContents.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogWarning("Archived {ContentCount} contents for locked user: {UserId}", 
                        userContents.Count, lockEvent.UserId);
                }

                // Clear user cache
                await _userCache.RemoveUserStateAsync(lockEvent.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserLockStatusChangedEvent for UserId: {UserId}", lockEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<RoleAddedToUserEvent> context)
    {
        var roleEvent = context.Message;
        _logger.LogInformation("Processing RoleAddedToUserEvent for User: {UserId} - Role: {RoleName}", 
            roleEvent.UserId, roleEvent.RoleName);

        try
        {
            // Update user role cache
            var userState = await _userCache.GetUserStateAsync(roleEvent.UserId);
            if (userState != null)
            {
                var currentRoles = userState.Roles.ToList();
                if (!currentRoles.Contains(roleEvent.RoleName))
                {
                    currentRoles.Add(roleEvent.RoleName);
                    await _userCache.UpdateUserRolesAsync(roleEvent.UserId, currentRoles);
                }
            }

            // Handle specific role additions
            await HandleRoleSpecificPermissions(roleEvent.UserId, roleEvent.RoleName, isAdding: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RoleAddedToUserEvent for UserId: {UserId}", roleEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<RoleRemovedFromUserEvent> context)
    {
        var roleEvent = context.Message;
        _logger.LogInformation("Processing RoleRemovedFromUserEvent for User: {UserId} - Role: {RoleName}", 
            roleEvent.UserId, roleEvent.RoleName);

        try
        {
            // Update user role cache
            var userState = await _userCache.GetUserStateAsync(roleEvent.UserId);
            if (userState != null)
            {
                var currentRoles = userState.Roles.ToList();
                if (currentRoles.Contains(roleEvent.RoleName))
                {
                    currentRoles.Remove(roleEvent.RoleName);
                    await _userCache.UpdateUserRolesAsync(roleEvent.UserId, currentRoles);
                }
            }

            // Handle specific role removals
            await HandleRoleSpecificPermissions(roleEvent.UserId, roleEvent.RoleName, isAdding: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RoleRemovedFromUserEvent for UserId: {UserId}", roleEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<RefreshTokenRevokedEvent> context)
    {
        var tokenEvent = context.Message;
        _logger.LogWarning("Processing RefreshTokenRevokedEvent for User: {UserId} - Reason: {Reason}", 
            tokenEvent.UserId, tokenEvent.Reason);

        try
        {
            // End content sessions for security
            await _userCache.RemoveUserStateAsync(tokenEvent.UserId);
            
            // TODO: End any active content streaming sessions, save content progress, etc.
            
            _logger.LogDebug("Ended content sessions due to token revocation for user: {UserId}", tokenEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RefreshTokenRevokedEvent for UserId: {UserId}", tokenEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserSessionsInvalidatedEvent> context)
    {
        var sessionEvent = context.Message;
        _logger.LogWarning("Processing UserSessionsInvalidatedEvent for User: {UserId} - Reason: {Reason}", 
            sessionEvent.UserId, sessionEvent.Reason);

        try
        {
            // Force end all content sessions
            await _userCache.RemoveUserStateAsync(sessionEvent.UserId);
            
            // TODO: End all content streaming, save all progress, clear temp data, etc.
            
            _logger.LogWarning("Force ended all content sessions for user: {UserId} due to session invalidation", 
                sessionEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserSessionsInvalidatedEvent for UserId: {UserId}", sessionEvent.UserId);
            throw;
        }
    }

    private async Task HandleRoleSpecificPermissions(Guid userId, string roleName, bool isAdding)
    {
        switch (roleName.ToLower())
        {
            case "moderator":
            case "admin":
                _logger.LogInformation("{Action} content moderation permissions for user: {UserId}", 
                    isAdding ? "Granted" : "Revoked", userId);
                // TODO: Update content moderation permissions
                break;
                
            case "premiumuser":
                _logger.LogInformation("{Action} premium content features for user: {UserId}", 
                    isAdding ? "Enabled" : "Disabled", userId);
                // TODO: Update premium content permissions, upload limits, etc.
                break;
                
            case "contentcreator":
                _logger.LogInformation("{Action} enhanced content creation features for user: {UserId}", 
                    isAdding ? "Enabled" : "Disabled", userId);
                // TODO: Update content creation permissions, analytics access, etc.
                break;
        }
        
        await Task.CompletedTask;
    }
}
