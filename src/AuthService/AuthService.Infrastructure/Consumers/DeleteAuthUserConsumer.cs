using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Saga;
using AuthService.Application.Commons.Interfaces;

namespace AuthService.Infrastructure.Consumers;

/// <summary>
/// Consumer để rollback AuthUser khi UserProfile creation fails
/// Đảm bảo data consistency trong distributed transaction
/// </summary>
public class DeleteAuthUserConsumer : IConsumer<DeleteAuthUser>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<DeleteAuthUserConsumer> _logger;

    public DeleteAuthUserConsumer(
        IIdentityService identityService,
        ILogger<DeleteAuthUserConsumer> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteAuthUser> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing DeleteAuthUser for UserId: {UserId}, Reason: {Reason}", 
            message.UserId, message.Reason);
        
        try
        {
            // Attempt to delete the user (single atomic operation)
            var result = await _identityService.DeleteUserAsync(message.UserId);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully deleted AuthUser {UserId} for rollback", message.UserId);
                
                // Publish success event
                await context.Publish<AuthUserDeleted>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    Success = true,
                    ErrorMessage = (string?)null
                });
            }
            else
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete AuthUser {UserId}: {Errors}", message.UserId, errorMessage);
                
                // Publish failure event
                await context.Publish<AuthUserDeleted>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    Success = false,
                    ErrorMessage = errorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting AuthUser {UserId}", message.UserId);
            
            // Publish failure event
            await context.Publish<AuthUserDeleted>(new
            {
                CorrelationId = message.CorrelationId,
                UserId = message.UserId,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}