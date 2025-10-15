using AuthService.Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Rpc;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// RPC Consumer for UpdateUserStatusRpcRequest
/// Updates user status in AuthService database via RPC pattern
/// Returns response for rollback capability
/// </summary>
public class UpdateUserStatusConsumer : IConsumer<UpdateUserStatusRpcRequest>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UpdateUserStatusConsumer> _logger;

    public UpdateUserStatusConsumer(
        UserManager<AppUser> userManager,
        ILogger<UpdateUserStatusConsumer> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateUserStatusRpcRequest> context)
    {
        var request = context.Message;

        _logger.LogInformation(
            "Processing UpdateUserStatusRpcRequest - UserId: {UserId}, NewStatus: {Status}",
            request.UserId, request.Status);

        try
        {
            // Get user from AuthService database
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found in AuthService - UserId: {UserId}", request.UserId);
                await context.RespondAsync(new UpdateUserStatusRpcResponse
                {
                    Success = false,
                    ErrorMessage = $"User not found: {request.UserId}",
                    UserId = request.UserId,
                    UpdatedStatus = request.Status
                });
                return;
            }

            var oldStatus = user.Status;

            // ✅ Update user status
            user.Status = request.Status;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = request.UpdatedBy;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user status in AuthService - UserId: {UserId}, Errors: {Errors}",
                    request.UserId, errors);
                
                await context.RespondAsync(new UpdateUserStatusRpcResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to update status: {errors}",
                    UserId = request.UserId,
                    UpdatedStatus = oldStatus // Return old status on failure
                });
                return;
            }

            _logger.LogInformation(
                "User status updated in AuthService - UserId: {UserId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
                request.UserId, oldStatus, request.Status);

            // ✅ Return success response
            await context.RespondAsync(new UpdateUserStatusRpcResponse
            {
                Success = true,
                ErrorMessage = null,
                UserId = request.UserId,
                UpdatedStatus = user.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UpdateUserStatusRpcRequest for user {UserId}", request.UserId);
            
            await context.RespondAsync(new UpdateUserStatusRpcResponse
            {
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}",
                UserId = request.UserId,
                UpdatedStatus = request.Status
            });
        }
    }
}
