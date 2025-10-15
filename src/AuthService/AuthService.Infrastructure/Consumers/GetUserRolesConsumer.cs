using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using SharedLibrary.Contracts.User.Rpc;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// RPC Consumer to get roles for multiple users
/// Uses Task.WhenAll for concurrent role fetching with separate scopes
/// Returns Dictionary for O(1) lookup performance
/// </summary>
public class GetUserRolesConsumer : IConsumer<GetUserRolesRequest>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<GetUserRolesConsumer> _logger;

    public GetUserRolesConsumer(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GetUserRolesConsumer> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetUserRolesRequest> context)
    {
        try
        {
            var userIds = context.Message.UserIds;
            
            _logger.LogInformation("RPC: GetUserRoles requested for {Count} users", userIds.Count);

            if (userIds == null || !userIds.Any())
            {
                await context.RespondAsync(new GetUserRolesResponse
                {
                    UserRoles = new Dictionary<Guid, List<string>>(),
                    Success = true
                });
                return;
            }

            // ✅ Create separate scope for each user to avoid DbContext threading issues
            // Each task gets its own UserManager and DbContext instance
            var userTasks = userIds.Select(async userId =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found in AuthService", userId);
                    return (userId, roles: new List<string>());
                }

                // Get roles for this user
                var roles = await userManager.GetRolesAsync(user);
                
                return (userId, roles: roles.ToList());
            });

            // ✅ Task.WhenAll for concurrent processing - each with its own scope
            var results = await Task.WhenAll(userTasks);

            // Build dictionary for O(1) lookup
            var userRolesDictionary = results.ToDictionary(
                r => r.userId,
                r => r.roles
            );

            _logger.LogInformation(
                "RPC: GetUserRoles completed - {Found}/{Total} users found",
                userRolesDictionary.Count(x => x.Value.Any()),
                userIds.Count);

            await context.RespondAsync(new GetUserRolesResponse
            {
                UserRoles = userRolesDictionary,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC: Error getting user roles");
            
            await context.RespondAsync(new GetUserRolesResponse
            {
                UserRoles = new Dictionary<Guid, List<string>>(),
                Success = false,
                ErrorMessage = "Failed to retrieve user roles"
            });
        }
    }
}
