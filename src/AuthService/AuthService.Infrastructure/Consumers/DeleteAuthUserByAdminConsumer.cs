using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Saga;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// Consumer for DeleteAuthUserByAdmin command (Compensating Action)
/// Deletes AuthUser when UserProfile update fails
/// Pattern: Similar to compensating actions in RegistrationSaga
/// </summary>
public class DeleteAuthUserByAdminConsumer : IConsumer<DeleteAuthUserByAdmin>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DeleteAuthUserByAdminConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteAuthUserByAdminConsumer(
        UserManager<AppUser> userManager,
        ILogger<DeleteAuthUserByAdminConsumer> logger,
        IPublishEndpoint publishEndpoint)
    {
        _userManager = userManager;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<DeleteAuthUserByAdmin> context)
    {
        var request = context.Message;
        
        try
        {
            _logger.LogWarning("Processing compensating action: DeleteAuthUserByAdmin - UserId: {UserId}, Reason: {Reason}, CorrelationId: {CorrelationId}",
                request.UserId, request.Reason, request.CorrelationId);

            // Find user to delete
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            
            if (user == null)
            {
                // User already deleted or never existed - treat as success (idempotent)
                _logger.LogWarning("AuthUser not found for deletion - UserId: {UserId}, CorrelationId: {CorrelationId} - Treating as success (idempotent)",
                    request.UserId, request.CorrelationId);
                
                await _publishEndpoint.Publish<AuthUserDeletedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserId = request.UserId,
                    Success = true,
                    ErrorMessage = (string?)null,
                    DeletedAt = DateTime.UtcNow
                });
                
                return;
            }

            // Delete user
            var result = await _userManager.DeleteAsync(user);
            
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete AuthUser - UserId: {UserId}, Errors: {Errors}, CorrelationId: {CorrelationId}",
                    request.UserId, errors, request.CorrelationId);
                
                await _publishEndpoint.Publish<AuthUserDeletedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserId = request.UserId,
                    Success = false,
                    ErrorMessage = $"Failed to delete user: {errors}",
                    DeletedAt = DateTime.UtcNow
                });
                
                return;
            }

            _logger.LogInformation("AuthUser deleted successfully (compensation) - UserId: {UserId}, Reason: {Reason}, CorrelationId: {CorrelationId}",
                request.UserId, request.Reason, request.CorrelationId);

            // Publish success event
            await _publishEndpoint.Publish<AuthUserDeletedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserId = request.UserId,
                Success = true,
                ErrorMessage = (string?)null,
                DeletedAt = DateTime.UtcNow
            });

            _logger.LogInformation("AuthUserDeletedByAdmin event published - UserId: {UserId}, CorrelationId: {CorrelationId}",
                request.UserId, request.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting AuthUser (compensation) - UserId: {UserId}, CorrelationId: {CorrelationId}",
                request.UserId, request.CorrelationId);

            await _publishEndpoint.Publish<AuthUserDeletedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserId = request.UserId,
                Success = false,
                ErrorMessage = ex.Message,
                DeletedAt = DateTime.UtcNow
            });
        }
    }
}
