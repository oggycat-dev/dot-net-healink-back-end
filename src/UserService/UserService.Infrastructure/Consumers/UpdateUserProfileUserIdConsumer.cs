using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Contracts.User.Saga;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Consumers;

/// <summary>
/// Consumer for UpdateUserProfileUserId command
/// Updates pre-created UserProfile with real UserId from AuthService
/// Pattern: Similar to CreateUserProfile but updates existing profile
/// </summary>
public class UpdateUserProfileUserIdConsumer : IConsumer<UpdateUserProfileUserId>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserProfileUserIdConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateUserProfileUserIdConsumer(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserProfileUserIdConsumer> logger,
        IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<UpdateUserProfileUserId> context)
    {
        var request = context.Message;
        
        try
        {
            _logger.LogInformation("Processing UpdateUserProfileUserId - ProfileId: {ProfileId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                request.UserProfileId, request.UserId, request.CorrelationId);

            // Find pre-created UserProfile
            var userProfile = await _unitOfWork.Repository<UserProfile>()
                .GetFirstOrDefaultAsync(u => u.Id == request.UserProfileId);

            if (userProfile == null)
            {
                _logger.LogError("UserProfile not found - ProfileId: {ProfileId}, CorrelationId: {CorrelationId}",
                    request.UserProfileId, request.CorrelationId);
                
                await _publishEndpoint.Publish<UserProfileUpdatedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserProfileId = request.UserProfileId,
                    Success = false,
                    ErrorMessage = "UserProfile not found"
                });
                
                return;
            }

            // ✅ IDEMPOTENCY CHECK: If UserId already matches and status is Active, return success
            if (userProfile.UserId == request.UserId && userProfile.Status == EntityStatusEnum.Active)
            {
                _logger.LogWarning("UserProfile already updated - ProfileId: {ProfileId}, UserId: {UserId}, Status: Active - Returning success (idempotent)",
                    request.UserProfileId, request.UserId);
                
                await _publishEndpoint.Publish<UserProfileUpdatedByAdmin>(new
                {
                    CorrelationId = request.CorrelationId,
                    UserProfileId = request.UserProfileId,
                    Success = true,
                    ErrorMessage = (string?)null
                });
                
                return;
            }

            // Update UserProfile with real UserId and set to Active
            userProfile.UserId = request.UserId;
            userProfile.Status = EntityStatusEnum.Active;
            userProfile.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<UserProfile>().Update(userProfile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("UserProfile updated successfully - ProfileId: {ProfileId}, UserId: {UserId}, Status: Active",
                request.UserProfileId, request.UserId);

            // Publish success event
            await _publishEndpoint.Publish<UserProfileUpdatedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserProfileId = request.UserProfileId,
                Success = true,
                ErrorMessage = (string?)null
            });

            _logger.LogInformation("UserProfileUpdatedByAdmin event published - ProfileId: {ProfileId}, CorrelationId: {CorrelationId}",
                request.UserProfileId, request.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating UserProfile - ProfileId: {ProfileId}, CorrelationId: {CorrelationId}",
                request.UserProfileId, request.CorrelationId);

            await _publishEndpoint.Publish<UserProfileUpdatedByAdmin>(new
            {
                CorrelationId = request.CorrelationId,
                UserProfileId = request.UserProfileId,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
