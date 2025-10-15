using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using SharedLibrary.Contracts.User.Rpc;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// RPC Consumer to update user email/phone in AuthService
/// Pattern: Request-Response with timeout (10s), no retry
/// UserService can rollback if this fails or times out
/// </summary>
public class UpdateUserInfoConsumer : IConsumer<UpdateUserInfoRpcRequest>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UpdateUserInfoConsumer> _logger;

    public UpdateUserInfoConsumer(
        UserManager<AppUser> userManager,
        ILogger<UpdateUserInfoConsumer> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateUserInfoRpcRequest> context)
    {
        const int MaxRetries = 3;
        var retryCount = 0;

        while (retryCount < MaxRetries)
        {
            try
            {
                var request = context.Message;
                
                _logger.LogInformation("RPC: UpdateUserInfo - UserId: {UserId}, Email: {Email}, Phone: {Phone}, Attempt: {Attempt}",
                    request.UserId, request.Email ?? "unchanged", request.PhoneNumber ?? "unchanged", retryCount + 1);

                // Find user - get fresh copy from database
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
                
                if (user == null)
                {
                    _logger.LogWarning("RPC: User not found - UserId: {UserId}", request.UserId);
                    
                    await context.RespondAsync(new UpdateUserInfoRpcResponse
                    {
                        Success = false,
                        ErrorMessage = "User not found in AuthService",
                        UserId = request.UserId
                    });
                    return;
                }

                var hasChanges = false;

                // Update email if provided
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    // Check if email already exists
                    var emailExists = await _userManager.Users
                        .AnyAsync(u => u.Email == request.Email && u.Id != request.UserId);

                    if (emailExists)
                    {
                        _logger.LogWarning("RPC: Email already in use - Email: {Email}", request.Email);
                        
                        await context.RespondAsync(new UpdateUserInfoRpcResponse
                        {
                            Success = false,
                            ErrorMessage = $"Email {request.Email} is already in use",
                            UserId = request.UserId
                        });
                        return;
                    }

                    user.Email = request.Email;
                    user.NormalizedEmail = request.Email.ToUpperInvariant();
                    user.UserName = request.Email; // Sync username with email
                    user.NormalizedUserName = request.Email.ToUpperInvariant();
                    hasChanges = true;

                    _logger.LogInformation("RPC: Email updated - UserId: {UserId}, NewEmail: {Email}",
                        request.UserId, request.Email);
                }

                // Update phone if provided
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
                {
                    user.PhoneNumber = request.PhoneNumber;
                    hasChanges = true;

                    _logger.LogInformation("RPC: Phone updated - UserId: {UserId}, NewPhone: {Phone}",
                        request.UserId, request.PhoneNumber);
                }

                if (!hasChanges)
                {
                    _logger.LogInformation("RPC: No changes needed - UserId: {UserId}", request.UserId);
                    
                    await context.RespondAsync(new UpdateUserInfoRpcResponse
                    {
                        Success = true,
                        UserId = request.UserId,
                        UpdatedEmail = user.Email,
                        UpdatedPhoneNumber = user.PhoneNumber
                    });
                    return;
                }

                // Save changes
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = request.UpdatedBy;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    
                    // Check if it's a concurrency error
                    if (errors.Contains("Optimistic concurrency failure") || errors.Contains("concurrency"))
                    {
                        retryCount++;
                        
                        if (retryCount < MaxRetries)
                        {
                            _logger.LogWarning(
                                "RPC: Concurrency conflict on attempt {Attempt} - UserId: {UserId}. Retrying...",
                                retryCount, request.UserId);
                            
                            // Wait before retrying (exponential backoff)
                            await Task.Delay(100 * retryCount);
                            continue; // Retry the operation
                        }
                    }
                    
                    _logger.LogError("RPC: Failed to update user - UserId: {UserId}, Errors: {Errors}",
                        request.UserId, errors);
                    
                    await context.RespondAsync(new UpdateUserInfoRpcResponse
                    {
                        Success = false,
                        ErrorMessage = $"Failed to update user: {errors}",
                        UserId = request.UserId
                    });
                    return;
                }

                _logger.LogInformation("RPC: User info updated successfully - UserId: {UserId}", request.UserId);

                await context.RespondAsync(new UpdateUserInfoRpcResponse
                {
                    Success = true,
                    UserId = request.UserId,
                    UpdatedEmail = user.Email,
                    UpdatedPhoneNumber = user.PhoneNumber
                });
                
                // Success - exit retry loop
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                
                if (retryCount < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "RPC: DbUpdateConcurrencyException on attempt {Attempt} - UserId: {UserId}. Retrying...",
                        retryCount, context.Message.UserId);
                    
                    // Wait before retrying (exponential backoff)
                    await Task.Delay(100 * retryCount);
                    continue; // Retry the operation
                }
                
                // Max retries reached
                _logger.LogError(ex, "RPC: Concurrency failure after {Retries} attempts - UserId: {UserId}",
                    MaxRetries, context.Message.UserId);
                
                await context.RespondAsync(new UpdateUserInfoRpcResponse
                {
                    Success = false,
                    ErrorMessage = $"Concurrency conflict after {MaxRetries} retries. Please try again.",
                    UserId = context.Message.UserId
                });
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC: Error updating user info - UserId: {UserId}", context.Message.UserId);
                
                await context.RespondAsync(new UpdateUserInfoRpcResponse
                {
                    Success = false,
                    ErrorMessage = "Internal error updating user info",
                    UserId = context.Message.UserId
                });
                return;
            }
        }
        
        // Should not reach here, but handle edge case
        _logger.LogError("RPC: Unexpected exit from retry loop - UserId: {UserId}", context.Message.UserId);
        await context.RespondAsync(new UpdateUserInfoRpcResponse
        {
            Success = false,
            ErrorMessage = "Unexpected error during update",
            UserId = context.Message.UserId
        });
    }
}
