using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.User.Saga;
using SharedLibrary.Commons.Repositories;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Consumers;

/// <summary>
/// Consumer để rollback UserProfile khi cần thiết
/// Đảm bảo data consistency trong distributed transaction
/// </summary>
public class DeleteUserProfileConsumer : IConsumer<DeleteUserProfile>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUserProfileConsumer> _logger;

    public DeleteUserProfileConsumer(
        IUnitOfWork unitOfWork,
        ILogger<DeleteUserProfileConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteUserProfile> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing DeleteUserProfile for UserProfileId: {UserProfileId}, UserId: {UserId}, Reason: {Reason}", 
            message.UserProfileId, message.UserId, message.Reason);
        
        try
        {
            var repository = _unitOfWork.Repository<UserProfile>();
            
            // Find the user profile
            var userProfile = await repository.GetFirstOrDefaultAsync(p => p.Id == message.UserProfileId);
            
            if (userProfile == null)
            {
                _logger.LogWarning("UserProfile {UserProfileId} not found for deletion", message.UserProfileId);
                
                // Even if not found, consider it success to avoid blocking the rollback
                await context.Publish<UserProfileDeleted>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserProfileId = message.UserProfileId,
                    UserId = message.UserId,
                    Success = true,
                    ErrorMessage = "UserProfile not found (already deleted or never created)"
                });
                return;
            }
            
            // Verify this profile belongs to the specified user
            if (userProfile.UserId != message.UserId)
            {
                _logger.LogError("UserProfile {UserProfileId} does not belong to UserId {UserId}", 
                    message.UserProfileId, message.UserId);
                
                await context.Publish<UserProfileDeleted>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserProfileId = message.UserProfileId,
                    UserId = message.UserId,
                    Success = false,
                    ErrorMessage = "UserProfile does not belong to the specified user"
                });
                return;
            }
            
            // Delete the user profile (single atomic operation)
            repository.Delete(userProfile);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Successfully deleted UserProfile {UserProfileId} for UserId {UserId}", 
                message.UserProfileId, message.UserId);
            
            // Publish success event
            await context.Publish<UserProfileDeleted>(new
            {
                CorrelationId = message.CorrelationId,
                UserProfileId = message.UserProfileId,
                UserId = message.UserId,
                Success = true,
                ErrorMessage = (string?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting UserProfile {UserProfileId} for UserId {UserId}", 
                message.UserProfileId, message.UserId);
            
            // Publish failure event
            await context.Publish<UserProfileDeleted>(new
            {
                CorrelationId = message.CorrelationId,
                UserProfileId = message.UserProfileId,
                UserId = message.UserId,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}