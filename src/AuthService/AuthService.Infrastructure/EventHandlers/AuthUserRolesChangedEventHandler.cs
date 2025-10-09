using AuthService.Application.Commons.Interfaces;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.SharedLibrary.Contracts.Events;

namespace AuthService.Infrastructure.EventHandlers;

/// <summary>
/// Event handler for UserRolesChangedEvent in AuthService
/// Updates user roles in BOTH database (for persistence) AND cache (for immediate effect)
/// </summary>
public class AuthUserRolesChangedEventHandler : IIntegrationEventHandler<UserRolesChangedEvent>
{
    private readonly IRoleService _roleService;
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<AuthUserRolesChangedEventHandler> _logger;

    public AuthUserRolesChangedEventHandler(
        IRoleService roleService,
        IUserStateCache userStateCache,
        ILogger<AuthUserRolesChangedEventHandler> logger)
    {
        _roleService = roleService;
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Handle(UserRolesChangedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling UserRolesChangedEvent for user {UserId}. New roles: {Roles}", 
                @event.UserId, string.Join(", ", @event.NewRoles));

            // Step 1: Update roles in DATABASE (for persistence across logins)
            var currentRoles = await _roleService.GetUserRolesAsync(@event.UserId, cancellationToken);
            
            // ADD new roles that user doesn't have
            // NOTE: We ONLY ADD, never remove existing roles to preserve base roles like "User"
            foreach (var newRole in @event.NewRoles)
            {
                if (!currentRoles.Contains(newRole, StringComparer.OrdinalIgnoreCase))
                {
                    await _roleService.AddRoleToUserAsync(@event.UserId, newRole, cancellationToken);
                }
            }

            _logger.LogInformation("Successfully updated roles in database for user {UserId}", @event.UserId);

            // Step 2: Update roles in REDIS CACHE (for immediate effect without re-login)
            // For cache, we merge existing roles with new roles to preserve all roles
            var allRoles = currentRoles.Union(@event.NewRoles).Distinct().ToList();
            await _userStateCache.UpdateUserRolesAsync(@event.UserId, allRoles);
            _logger.LogInformation("Successfully updated roles in cache for user {UserId}. All roles: {AllRoles}", 
                @event.UserId, string.Join(", ", allRoles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserRolesChangedEvent for user {UserId}", @event.UserId);
            throw;
        }
    }
}
