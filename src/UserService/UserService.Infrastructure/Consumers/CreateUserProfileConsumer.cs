using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Contracts.User.Saga;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý CreateUserProfile command từ Registration Saga
/// </summary>
public class CreateUserProfileConsumer : IConsumer<CreateUserProfile>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserProfileConsumer> _logger;

    public CreateUserProfileConsumer(
        IUnitOfWork unitOfWork,
        ILogger<CreateUserProfileConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateUserProfile> context)
    {
        var message = context.Message;
        
        try
        {
            _logger.LogInformation("Processing CreateUserProfile for UserId: {UserId}, email: {Email}, CorrelationId: {CorrelationId}", 
                message.UserId, message.Email, message.CorrelationId);

            // Check if user profile already exists
            var existingUser = await _unitOfWork.Repository<UserProfile>().GetFirstOrDefaultAsync(
                u => u.UserId == message.UserId || u.Email.ToLower() == message.Email.ToLower());

            if (existingUser != null)
            {
                _logger.LogWarning("User profile already exists for UserId: {UserId}, email: {Email}, CorrelationId: {CorrelationId}", 
                    message.UserId, message.Email, message.CorrelationId);

                // If user profile already exists, consider it a success to avoid duplicate creation
                await context.Publish<UserProfileCreated>(new
                {
                    CorrelationId = message.CorrelationId,
                    UserProfileId = existingUser.Id,
                    UserId = existingUser.UserId,
                    Success = true,
                    ErrorMessage = (string?)null,
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            // Create new UserProfile
            var newUserProfile = new UserProfile
            {
                UserId = message.UserId, // Foreign Key to AppUser in AuthService
                Email = message.Email,
                FullName = message.FullName,
                PhoneNumber = message.PhoneNumber,
                Status = EntityStatusEnum.Active,
            };

            // Initialize entity (assuming there's a base entity method)
            if (newUserProfile is SharedLibrary.Commons.Entities.BaseEntity baseEntity)
            {
                baseEntity.InitializeEntity(newUserProfile.UserId);
            }

            // Add user profile to repository (single atomic operation)
            await _unitOfWork.Repository<UserProfile>().AddAsync(newUserProfile);
            
            // Save changes
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User profile created successfully for UserId: {UserId}, email: {Email}, ProfileId: {ProfileId}, CorrelationId: {CorrelationId}", 
                message.UserId, message.Email, newUserProfile.Id, message.CorrelationId);

            // Publish success response
            await context.Publish<UserProfileCreated>(new
            {
                CorrelationId = message.CorrelationId,
                UserProfileId = newUserProfile.Id,
                UserId = newUserProfile.UserId,
                Success = true,
                ErrorMessage = (string?)null,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user profile for UserId: {UserId}, email: {Email}, CorrelationId: {CorrelationId}", 
                message.UserId, message.Email, message.CorrelationId);

            // Publish failure response
            await context.Publish<UserProfileCreated>(new
            {
                CorrelationId = message.CorrelationId,
                UserProfileId = Guid.Empty,
                UserId = message.UserId,
                Success = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}