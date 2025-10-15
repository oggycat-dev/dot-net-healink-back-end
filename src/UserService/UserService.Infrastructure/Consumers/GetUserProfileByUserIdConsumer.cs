using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Repositories;
using SharedLibrary.Contracts.User.Requests;
using SharedLibrary.Contracts.User.Responses;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Consumers;

/// <summary>
/// Consumer to handle UserProfile query by UserId (from AuthService)
/// Used during login to get UserProfileId for caching
/// </summary>
public class GetUserProfileByUserIdConsumer : IConsumer<GetUserProfileByUserIdRequest>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserProfileByUserIdConsumer> _logger;

    public GetUserProfileByUserIdConsumer(
        IUnitOfWork unitOfWork,
        ILogger<GetUserProfileByUserIdConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetUserProfileByUserIdRequest> context)
    {
        var request = context.Message;
        
        _logger.LogInformation(
            "Querying UserProfile for UserId={UserId}",
            request.UserId);

        try
        {
            // Query UserProfile by UserId (foreign key from AuthService)
            var userProfile = await _unitOfWork.Repository<UserProfile>()
                .GetFirstOrDefaultAsync(up => up.UserId == request.UserId);

            if (userProfile == null)
            {
                _logger.LogWarning(
                    "UserProfile not found for UserId={UserId}",
                    request.UserId);

                // Return response with Found=false
                await context.RespondAsync(new GetUserProfileByUserIdResponse
                {
                    Found = false,
                    UserId = request.UserId,
                    UserProfileId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty
                });
                return;
            }

            _logger.LogInformation(
                "UserProfile found: UserProfileId={UserProfileId}, UserId={UserId}",
                userProfile.Id, request.UserId);

            // Return UserProfileId (this is UserProfile.Id)
            await context.RespondAsync(new GetUserProfileByUserIdResponse
            {
                Found = true,
                UserProfileId = userProfile.Id, // ✅ This is the UserProfileId!
                UserId = userProfile.UserId ?? Guid.Empty,     // Foreign key to AppUser (use Empty if null)
                Email = userProfile.Email,
                FullName = userProfile.FullName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error querying UserProfile for UserId={UserId}",
                request.UserId);

            // Return error response
            await context.RespondAsync(new GetUserProfileByUserIdResponse
            {
                Found = false,
                UserId = request.UserId,
                UserProfileId = Guid.Empty,
                Email = string.Empty,
                FullName = string.Empty
            });
        }
    }
}

