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

namespace UserService.Application.Features.Users.Commands.UpdateUserRoles;

/// <summary>
/// Handler for updating user roles
/// Pattern: RPC sync to AuthService → Update cache → Log activity
/// If AuthService RPC fails/timeout, return error (no cache/log update)
/// Timeout: 5 seconds
/// </summary>
public class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UpdateUserRolesRpcRequest> _authClient;
    private readonly IUserStateCache _userStateCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserRolesCommandHandler> _logger;

    public UpdateUserRolesCommandHandler(
        IUnitOfWork unitOfWork,
        IRequestClient<UpdateUserRolesRpcRequest> authClient,
        IUserStateCache userStateCache,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserRolesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _authClient = authClient;
        _userStateCache = userStateCache;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating user roles - UserId: {UserId}, Add: {Add}, Remove: {Remove}",
                command.UserId, 
                string.Join(",", command.RolesToAdd.Select(r => r.ToString())), 
                string.Join(",", command.RolesToRemove.Select(r => r.ToString())));

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

            if (userProfile.Status != EntityStatusEnum.Active)
            {
                _logger.LogWarning("UserProfile is not active - UserId: {UserId}, Status: {Status}", 
                    command.UserId, userProfile.Status);
                return Result.Failure("User is not active", ErrorCodeEnum.ValidationFailed);
            }

            if (!command.RolesToAdd.Any() && !command.RolesToRemove.Any())
            {
                _logger.LogInformation("No role changes requested - UserId: {UserId}", command.UserId);
                return Result.Success("No changes requested");
            }

            // Get current roles from cache (if exists) or empty list
            var userState = await _userStateCache.GetUserStateAsync(command.UserId);
            var currentRoles = userState?.Roles.ToList() ?? new List<string>();
            var oldRoles = new List<string>(currentRoles);

            _logger.LogInformation("Current roles from cache - UserId: {UserId}, Roles: {Roles}", 
                command.UserId, currentRoles.Any() ? string.Join(",", currentRoles) : "empty");

            // ✅ STEP 1: Calculate new roles (for RPC request)
            var rolesToAddStrings = command.RolesToAdd.Select(r => r.ToString()).ToList();
            var rolesToRemoveStrings = command.RolesToRemove.Select(r => r.ToString()).ToList();
            
            var newRoles = currentRoles
                .Except(rolesToRemoveStrings)
                .Union(rolesToAddStrings)
                .Distinct()
                .ToList();

            // ✅ STEP 2: Sync to AuthService via RPC (MUST succeed before cache/log)
            try
            {
                _logger.LogInformation("Syncing user roles to AuthService via RPC - UserId: {UserId}", command.UserId);

                var rpcResponse = await _authClient.GetResponse<UpdateUserRolesRpcResponse>(
                    new UpdateUserRolesRpcRequest
                    {
                        UserId = command.UserId,
                        OldRoles = currentRoles,
                        NewRoles = newRoles,
                        AddedRoles = rolesToAddStrings,
                        RemovedRoles = rolesToRemoveStrings,
                        UpdatedBy = currentUserId.Value
                    },
                    timeout: RequestTimeout.After(s: 5), // ✅ 5s timeout, no retry
                    cancellationToken: cancellationToken);

                if (!rpcResponse.Message.Success)
                {
                    // ❌ AuthService update failed → DO NOT update cache or log
                    _logger.LogError("AuthService RPC failed - UserId: {UserId}, Error: {Error}",
                        command.UserId, rpcResponse.Message.ErrorMessage);

                    return Result.Failure(
                        $"Failed to sync with AuthService: {rpcResponse.Message.ErrorMessage}",
                        ErrorCodeEnum.InternalError);
                }

                _logger.LogInformation("AuthService sync successful - UserId: {UserId}, UpdatedRoles: {Roles}",
                    command.UserId, string.Join(",", rpcResponse.Message.UpdatedRoles));
                
                // Use roles from AuthService response as source of truth
                newRoles = rpcResponse.Message.UpdatedRoles;
            }
            catch (RequestTimeoutException ex)
            {
                // ❌ RPC Timeout → DO NOT update cache or log
                _logger.LogError(ex, "AuthService RPC timeout - UserId: {UserId}", command.UserId);

                return Result.Failure(
                    "Failed to sync with AuthService: timeout after 5 seconds",
                    ErrorCodeEnum.InternalError);
            }

            // ✅ STEP 3: Update or create cache with new roles (ONLY after AuthService success)
            var cachedUserState = await _userStateCache.GetUserStateAsync(command.UserId);
            
            if (cachedUserState != null)
            {
                // Update existing cache
                var updatedState = cachedUserState with 
                { 
                    Roles = newRoles,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(updatedState);
                _logger.LogInformation("Cache updated with new roles - UserId: {UserId}, Roles: {Roles}",
                    command.UserId, string.Join(",", newRoles));
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
                    Roles = newRoles,
                    Status = userProfile.Status,
                    Subscription = null, // Will be updated when user logs in or subscribes
                    LastLoginAt = DateTime.MinValue, // Never logged in
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(newUserState);
                _logger.LogInformation("Cache created with new roles - UserId: {UserId}, Roles: {Roles}",
                    command.UserId, string.Join(",", newRoles));
            }

            // ✅ STEP 4: Log user activity (ONLY after AuthService success)
            var activityLog = new UserActivityLog
            {
                UserId = userProfile.Id,
                ActivityType = "UserRolesUpdated",
                Description = $"User roles updated. Added: [{string.Join(", ", rolesToAddStrings)}], Removed: [{string.Join(", ", rolesToRemoveStrings)}]",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OldRoles = oldRoles,
                    NewRoles = newRoles,
                    AddedRoles = rolesToAddStrings,
                    RemovedRoles = rolesToRemoveStrings,
                    UpdatedBy = currentUserId.Value
                }),
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent,
                OccurredAt = DateTime.UtcNow
            };
            activityLog.InitializeEntity();

            await activityLogRepo.AddAsync(activityLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User roles update completed - UserId: {UserId}, NewRoles: {Roles}",
                command.UserId, string.Join(",", newRoles));

            return Result.Success("User roles updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user roles - UserId: {UserId}", command.UserId);
            return Result.Failure("Failed to update user roles", ErrorCodeEnum.InternalError);
        }
    }
}