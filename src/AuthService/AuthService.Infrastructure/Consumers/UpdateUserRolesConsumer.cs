using AuthService.Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Rpc;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// RPC Consumer for UpdateUserRolesRpcRequest
/// Updates user roles in AuthService database and returns response
/// Pattern: Request-Response with success/failure indication
/// </summary>
public class UpdateUserRolesConsumer : IConsumer<UpdateUserRolesRpcRequest>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<UpdateUserRolesConsumer> _logger;

    public UpdateUserRolesConsumer(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger<UpdateUserRolesConsumer> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateUserRolesRpcRequest> context)
    {
        var request = context.Message;

        _logger.LogInformation(
            "RPC: Processing UpdateUserRolesRpcRequest - UserId: {UserId}, AddedRoles: {AddedRoles}, RemovedRoles: {RemovedRoles}",
            request.UserId, string.Join(",", request.AddedRoles), string.Join(",", request.RemovedRoles));

        try
        {
            // Get user from AuthService database
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("RPC: User not found in AuthService - UserId: {UserId}", request.UserId);
                await context.RespondAsync(new UpdateUserRolesRpcResponse
                {
                    Success = false,
                    ErrorMessage = "User not found in AuthService",
                    UserId = request.UserId,
                    UpdatedRoles = new List<string>()
                });
                return;
            }

            // ✅ STEP 1: Remove roles
            if (request.RemovedRoles.Any())
            {
                _logger.LogInformation("RPC: Removing roles: {Roles} from user {UserId}", 
                    string.Join(",", request.RemovedRoles), request.UserId);

                var removeResult = await _userManager.RemoveFromRolesAsync(user, request.RemovedRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("RPC: Failed to remove roles from user {UserId}: {Errors}",
                        request.UserId, errors);
                    
                    await context.RespondAsync(new UpdateUserRolesRpcResponse
                    {
                        Success = false,
                        ErrorMessage = $"Failed to remove roles: {errors}",
                        UserId = request.UserId,
                        UpdatedRoles = new List<string>()
                    });
                    return;
                }

                _logger.LogInformation("RPC: Successfully removed roles from user {UserId}", request.UserId);
            }

            // ✅ STEP 2: Add roles (validate and create if needed)
            if (request.AddedRoles.Any())
            {
                _logger.LogInformation("RPC: Adding roles: {Roles} to user {UserId}", 
                    string.Join(",", request.AddedRoles), request.UserId);

                // Validate roles exist, create if missing
                foreach (var roleName in request.AddedRoles)
                {
                    var roleExists = await _roleManager.RoleExistsAsync(roleName);
                    if (!roleExists)
                    {
                        _logger.LogWarning("RPC: Role {Role} does not exist, creating it", roleName);
                        var createRoleResult = await _roleManager.CreateAsync(new AppRole { Name = roleName });
                        if (!createRoleResult.Succeeded)
                        {
                            var errors = string.Join(", ", createRoleResult.Errors.Select(e => e.Description));
                            _logger.LogError("RPC: Failed to create role {Role}: {Errors}", roleName, errors);
                            
                            await context.RespondAsync(new UpdateUserRolesRpcResponse
                            {
                                Success = false,
                                ErrorMessage = $"Failed to create role {roleName}: {errors}",
                                UserId = request.UserId,
                                UpdatedRoles = new List<string>()
                            });
                            return;
                        }
                    }
                }

                var addResult = await _userManager.AddToRolesAsync(user, request.AddedRoles);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    _logger.LogError("RPC: Failed to add roles to user {UserId}: {Errors}",
                        request.UserId, errors);
                    
                    await context.RespondAsync(new UpdateUserRolesRpcResponse
                    {
                        Success = false,
                        ErrorMessage = $"Failed to add roles: {errors}",
                        UserId = request.UserId,
                        UpdatedRoles = new List<string>()
                    });
                    return;
                }

                _logger.LogInformation("RPC: Successfully added roles to user {UserId}", request.UserId);
            }

            // ✅ STEP 3: Get final roles and respond with success
            var currentRoles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation(
                "RPC: UpdateUserRoles completed successfully - UserId: {UserId}, CurrentRoles: {Roles}",
                request.UserId, string.Join(",", currentRoles));

            await context.RespondAsync(new UpdateUserRolesRpcResponse
            {
                Success = true,
                ErrorMessage = null,
                UserId = request.UserId,
                UpdatedRoles = currentRoles.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC: Error processing UpdateUserRolesRpcRequest for user {UserId}", request.UserId);
            
            await context.RespondAsync(new UpdateUserRolesRpcResponse
            {
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}",
                UserId = request.UserId,
                UpdatedRoles = new List<string>()
            });
        }
    }
}
