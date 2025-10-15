using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Commons.Services;
using SharedLibrary.Contracts.User.Rpc;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Users.Commands.UpdateUserStatus;

/// <summary>
/// Handler for updating user status
/// Pattern: Update UserService → RPC sync to AuthService → Update cache → Log activity
/// If AuthService RPC fails/timeout, rollback UserService changes
/// Timeout: 5 seconds
/// </summary>
public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UpdateUserStatusRpcRequest> _authClient;
    private readonly IUserStateCache _userStateCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserStatusCommandHandler> _logger;

    public UpdateUserStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IRequestClient<UpdateUserStatusRpcRequest> authClient,
        IUserStateCache userStateCache,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _authClient = authClient;
        _userStateCache = userStateCache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserStatusCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating user status - UserId: {UserId}, NewStatus: {Status}",
                command.UserId, command.Status);

            // ✅ VALIDATION: Validate current user performing the operation via ICurrentUserService
            var (isCurrentUserValid, currentUserId) = await _currentUserService.IsUserValidAsync();
            if (!isCurrentUserValid || !currentUserId.HasValue)
            {
                _logger.LogWarning("Current user is invalid or not authenticated");
                return Result.Failure("Unauthorized - Invalid user session", ErrorCodeEnum.Unauthorized);
            }

            var userProfileRepo = _unitOfWork.Repository<UserProfile>();
            var activityLogRepo = _unitOfWork.Repository<UserActivityLog>();

            // ✅ VALIDATION: Check target user in DB (not cache - user may not have logged in yet)
            var userProfile = await userProfileRepo.GetFirstOrDefaultAsync(u => u.UserId == command.UserId);
            if (userProfile == null)
            {
                _logger.LogWarning("UserProfile not found in DB - UserId: {UserId}", command.UserId);
                return Result.Failure("User profile not found", ErrorCodeEnum.NotFound);
            }

            var oldStatus = userProfile.Status;

            if (oldStatus == command.Status)
            {
                _logger.LogInformation("Status unchanged - UserId: {UserId}, Status: {Status}",
                    command.UserId, command.Status);
                return Result.Success("Status unchanged");
            }

            // ✅ STEP 1: Update UserService DB first (will rollback if AuthService fails)
            userProfile.Status = command.Status;
            userProfile.UpdatedAt = DateTime.UtcNow;
            userProfile.UpdatedBy = currentUserId.Value; // Use validated currentUserId

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UserService status updated - UserId: {UserId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
                command.UserId, oldStatus, command.Status);

            // ✅ STEP 2: Sync to AuthService via RPC
            try
            {
                _logger.LogInformation("Syncing user status to AuthService via RPC - UserId: {UserId}", command.UserId);

                var rpcResponse = await _authClient.GetResponse<UpdateUserStatusRpcResponse>(
                    new UpdateUserStatusRpcRequest
                    {
                        UserId = command.UserId,
                        Status = command.Status,
                        Reason = command.Reason,
                        UpdatedBy = currentUserId.Value
                    },
                    timeout: RequestTimeout.After(s: 5), // ✅ 5s timeout, no retry
                    cancellationToken: cancellationToken);

                if (!rpcResponse.Message.Success)
                {
                    // ❌ AuthService update failed → ROLLBACK UserService changes
                    _logger.LogError("AuthService RPC failed, rolling back - UserId: {UserId}, Error: {Error}",
                        command.UserId, rpcResponse.Message.ErrorMessage);

                    // Rollback to old status
                    userProfile.Status = oldStatus;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result.Failure(
                        $"Failed to sync with AuthService: {rpcResponse.Message.ErrorMessage}",
                        ErrorCodeEnum.InternalError);
                }

                _logger.LogInformation("AuthService sync successful - UserId: {UserId}", command.UserId);
            }
            catch (RequestTimeoutException ex)
            {
                // ❌ RPC Timeout → ROLLBACK UserService changes
                _logger.LogError(ex, "AuthService RPC timeout, rolling back - UserId: {UserId}", command.UserId);

                // Rollback to old status
                userProfile.Status = oldStatus;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure(
                    "Failed to sync with AuthService: timeout after 5 seconds",
                    ErrorCodeEnum.InternalError);
            }

            // ✅ STEP 3: Update or create cache with new status (ONLY after AuthService success)
            var cachedUserState = await _userStateCache.GetUserStateAsync(command.UserId);
            if (cachedUserState != null)
            {
                // Update existing cache
                var updatedState = cachedUserState with 
                { 
                    Status = command.Status,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(updatedState);
                _logger.LogInformation("Cache updated with new status - UserId: {UserId}, Status: {Status}",
                    command.UserId, command.Status);
            }
            else
            {
                // Create new cache entry (user never logged in before)
                _logger.LogInformation("Creating new cache entry for user - UserId: {UserId}", command.UserId);
                var newUserState = new UserStateInfo
                {
                    UserId = command.UserId,
                    UserProfileId = userProfile.Id,
                    Email = userProfile.Email,
                    Roles = new List<string>(), // Will be populated from AuthService on first login
                    Status = command.Status,
                    Subscription = null,
                    LastLoginAt = DateTime.MinValue,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(newUserState);
                _logger.LogInformation("Cache created with status - UserId: {UserId}, Status: {Status}",
                    command.UserId, command.Status);
            }

            // ✅ STEP 4: Log user activity
            var activityLog = new UserActivityLog
            {
                UserId = userProfile.Id,
                ActivityType = "UserStatusUpdated",
                Description = $"User status changed from {oldStatus} to {command.Status}. Reason: {command.Reason ?? "N/A"}",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OldStatus = oldStatus.ToString(),
                    NewStatus = command.Status.ToString(),
                    Reason = command.Reason,
                    UpdatedBy = currentUserId.Value // Use validated currentUserId
                }),
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent,
                OccurredAt = DateTime.UtcNow
            };
            activityLog.InitializeEntity();

            await activityLogRepo.AddAsync(activityLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User status update completed - UserId: {UserId}, NewStatus: {Status}",
                command.UserId, command.Status);

            return Result.Success($"User status updated to {command.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status - UserId: {UserId}", command.UserId);
            return Result.Failure("Failed to update user status", ErrorCodeEnum.InternalError);
        }
    }
}
