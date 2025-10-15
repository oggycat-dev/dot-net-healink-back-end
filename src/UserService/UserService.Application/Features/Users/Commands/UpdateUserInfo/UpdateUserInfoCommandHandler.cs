using AutoMapper;
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
using UserService.Application.Commons.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Features.Users.Commands.UpdateUserInfo;

/// <summary>
/// Handler for updating user information
/// Pattern: Update UserService → RPC sync to AuthService → Update cache → Log activity
/// If AuthService RPC fails/timeout, rollback UserService changes
/// </summary>
public class UpdateUserInfoCommandHandler : IRequestHandler<UpdateUserInfoCommand, Result<UserProfileResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<UpdateUserInfoRpcRequest> _authClient;
    private readonly IUserStateCache _userStateCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserInfoCommandHandler> _logger;

    public UpdateUserInfoCommandHandler(
        IUnitOfWork unitOfWork,
        IRequestClient<UpdateUserInfoRpcRequest> authClient,
        IUserStateCache userStateCache,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<UpdateUserInfoCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _authClient = authClient;
        _userStateCache = userStateCache;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserProfileResponse>> Handle(UpdateUserInfoCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // ⚠️ IMPORTANT: command.UserId = Auth Service User ID (AppUser.Id), NOT UserProfile.Id
            // Used for: Cache key, UserProfile.UserId query (FK), RPC to AuthService
            _logger.LogInformation("Updating user info - Auth UserId: {UserId}", command.UserId);

            // ✅ VALIDATION: Validate current user performing the operation via ICurrentUserService
            var (isCurrentUserValid, currentUserId) = await _currentUserService.IsUserValidAsync();
            if (!isCurrentUserValid || !currentUserId.HasValue)
            {
                _logger.LogWarning("Current user is invalid or not authenticated");
                return Result<UserProfileResponse>.Failure("Unauthorized - Invalid user session", ErrorCodeEnum.Unauthorized);
            }

            var userProfileRepo = _unitOfWork.Repository<UserProfile>();
            var activityLogRepo = _unitOfWork.Repository<UserActivityLog>();

            // ✅ VALIDATION: Check target user in DB (not cache - user may not have logged in yet)
            // UserProfile.UserId = Auth Service User ID (FK to AppUser.Id)
            var userProfile = await userProfileRepo.GetFirstOrDefaultAsync(
                u => u.UserId == command.UserId);

            if (userProfile == null)
            {
                _logger.LogWarning("UserProfile not found - Auth UserId: {UserId}", command.UserId);
                return Result<UserProfileResponse>.Failure("User profile not found", ErrorCodeEnum.NotFound);
            }

            if (userProfile.Status != EntityStatusEnum.Active)
            {
                _logger.LogWarning("UserProfile is not active - UserId: {UserId}, Status: {Status}", 
                    command.UserId, userProfile.Status);
                return Result<UserProfileResponse>.Failure("User is not active", ErrorCodeEnum.ValidationFailed);
            }

            // Store old values for rollback and activity log
            var oldEmail = userProfile.Email;
            var oldPhone = userProfile.PhoneNumber;
            var oldFullName = userProfile.FullName;
            var oldAddress = userProfile.Address;

            var hasChanges = false;
            var needsAuthServiceSync = false;

            // ✅ STEP 1: Update UserService fields
            if (!string.IsNullOrWhiteSpace(command.Request.FullName) && command.Request.FullName != userProfile.FullName)
            {
                userProfile.FullName = command.Request.FullName;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.Address) && command.Request.Address != userProfile.Address)
            {
                userProfile.Address = command.Request.Address;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.Email) && command.Request.Email != userProfile.Email)
            {
                // Check if email already exists (except current user)
                var emailExists = await userProfileRepo.GetFirstOrDefaultAsync(
                    u => u.Email == command.Request.Email && u.UserId != command.UserId);

                if (emailExists != null)
                {
                    _logger.LogWarning("Email already in use - Email: {Email}", command.Request.Email);
                    return Result<UserProfileResponse>.Failure("Email already in use", ErrorCodeEnum.ValidationFailed);
                }

                userProfile.Email = command.Request.Email;
                hasChanges = true;
                needsAuthServiceSync = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.PhoneNumber) && command.Request.PhoneNumber != userProfile.PhoneNumber)
            {
                userProfile.PhoneNumber = command.Request.PhoneNumber;
                hasChanges = true;
                needsAuthServiceSync = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for user - UserId: {UserId}", command.UserId);
                
                // Get roles from cache or empty list
                var userState = await _userStateCache.GetUserStateAsync(command.UserId);
                var roles = userState?.Roles.ToList() ?? new List<string>();
                
                var response = _mapper.Map<UserProfileResponse>(userProfile);
                response.Roles = roles;
                return Result<UserProfileResponse>.Success(response);
            }

            userProfile.UpdatedAt = DateTime.UtcNow;
            userProfile.UpdatedBy = currentUserId.Value; // Use validated currentUserId

            // ✅ STEP 2: Save to UserService first (will rollback if AuthService fails)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UserService updated - UserId: {UserId}, NeedsAuthSync: {NeedsAuthSync}",
                command.UserId, needsAuthServiceSync);

            // ✅ STEP 3: Sync to AuthService via RPC if email/phone changed
            if (needsAuthServiceSync)
            {
                try
                {
                    _logger.LogInformation("Syncing user info to AuthService via RPC - UserId: {UserId}", command.UserId);

                    var rpcResponse = await _authClient.GetResponse<UpdateUserInfoRpcResponse>(
                        new UpdateUserInfoRpcRequest
                        {
                            UserId = command.UserId,
                            Email = command.Request.Email,
                            PhoneNumber = command.Request.PhoneNumber,
                            UpdatedBy = currentUserId.Value // Use validated currentUserId
                        },
                        timeout: RequestTimeout.After(s: 10), // ✅ 10s timeout, no retry
                        cancellationToken: cancellationToken);

                    if (!rpcResponse.Message.Success)
                    {
                        // ❌ AuthService update failed → ROLLBACK UserService changes
                        _logger.LogError("AuthService RPC failed, rolling back - UserId: {UserId}, Error: {Error}",
                            command.UserId, rpcResponse.Message.ErrorMessage);

                        // Rollback to old values
                        userProfile.Email = oldEmail;
                        userProfile.PhoneNumber = oldPhone;
                        userProfile.FullName = oldFullName;
                        userProfile.Address = oldAddress;

                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        return Result<UserProfileResponse>.Failure(
                            $"Failed to sync with AuthService: {rpcResponse.Message.ErrorMessage}",
                            ErrorCodeEnum.InternalError);
                    }

                    _logger.LogInformation("AuthService sync successful - UserId: {UserId}", command.UserId);
                }
                catch (RequestTimeoutException ex)
                {
                    // ❌ RPC Timeout → ROLLBACK UserService changes
                    _logger.LogError(ex, "AuthService RPC timeout, rolling back - UserId: {UserId}", command.UserId);

                    // Rollback to old values
                    userProfile.Email = oldEmail;
                    userProfile.PhoneNumber = oldPhone;
                    userProfile.FullName = oldFullName;
                    userProfile.Address = oldAddress;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<UserProfileResponse>.Failure(
                        "Failed to sync with AuthService: timeout after 10 seconds",
                        ErrorCodeEnum.InternalError);
                }
            }

            // ✅ STEP 4: Update cache immediately (don't trust JWT)
            // Get current cache, update fields using 'with' expression, set back
            // Cache key = Auth User ID
            var cachedUserState = await _userStateCache.GetUserStateAsync(command.UserId);
            if (cachedUserState != null)
            {
                // Update existing cache
                var updatedState = cachedUserState with 
                { 
                    Email = userProfile.Email,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(updatedState);
                _logger.LogInformation("Cache updated with new email - Auth UserId: {UserId}, Email: {Email}", 
                    command.UserId, userProfile.Email);
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
                    Status = userProfile.Status,
                    Subscription = null,
                    LastLoginAt = DateTime.MinValue,
                    CacheUpdatedAt = DateTime.UtcNow
                };
                await _userStateCache.SetUserStateAsync(newUserState);
                _logger.LogInformation("Cache created - Auth UserId: {UserId}, Email: {Email}", 
                    command.UserId, userProfile.Email);
            }

            // ✅ STEP 5: Log user activity
            // ActivityLog.UserId = UserProfile.Id (PK) - NOT Auth User ID
            var changes = new List<string>();
            if (oldFullName != userProfile.FullName) changes.Add($"FullName: {oldFullName} → {userProfile.FullName}");
            if (oldEmail != userProfile.Email) changes.Add($"Email: {oldEmail} → {userProfile.Email}");
            if (oldPhone != userProfile.PhoneNumber) changes.Add($"Phone: {oldPhone} → {userProfile.PhoneNumber}");
            if (oldAddress != userProfile.Address) changes.Add($"Address: {oldAddress} → {userProfile.Address}");

            var activityLog = new UserActivityLog
            {
                UserId = userProfile.Id,
                ActivityType = "UserInfoUpdated",
                Description = $"User info updated: {string.Join(", ", changes)}",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OldEmail = oldEmail,
                    NewEmail = userProfile.Email,
                    OldPhone = oldPhone,
                    NewPhone = userProfile.PhoneNumber,
                    Changes = changes
                }),
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent,
                OccurredAt = DateTime.UtcNow
            };
            activityLog.InitializeEntity();

            await activityLogRepo.AddAsync(activityLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User info update completed - UserId: {UserId}, Changes: {Changes}",
                command.UserId, string.Join(", ", changes));

            // Get roles for final response
            var finalUserState = await _userStateCache.GetUserStateAsync(command.UserId);
            var finalRoles = finalUserState?.Roles.ToList() ?? new List<string>();

            var finalResponse = _mapper.Map<UserProfileResponse>(userProfile);
            finalResponse.Roles = finalRoles;

            return Result<UserProfileResponse>.Success(finalResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user info - UserId: {UserId}", command.UserId);
            return Result<UserProfileResponse>.Failure("Failed to update user info", ErrorCodeEnum.InternalError);
        }
    }
}
