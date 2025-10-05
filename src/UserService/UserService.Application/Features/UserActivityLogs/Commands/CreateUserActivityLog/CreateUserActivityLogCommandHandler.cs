using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Repositories;
using UserService.Domain.Entities;

namespace UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;

/// <summary>
/// Handler để xử lý việc ghi log hoạt động của user
/// Clean Architecture: Command Handler chứa business logic và sử dụng Repository
/// </summary>
public class CreateUserActivityLogCommandHandler : IRequestHandler<CreateUserActivityLogCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserActivityLogCommandHandler> _logger;

    public CreateUserActivityLogCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateUserActivityLogCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CreateUserActivityLogCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating user activity log for Auth UserId {UserId}, Activity: {ActivityType}",
                request.UserId,
                request.ActivityType);

            var userProfileRepository = _unitOfWork.Repository<UserProfile>();
            var activityLogRepository = _unitOfWork.Repository<UserActivityLog>();

            // 1. Find UserProfile by Auth UserId
            var userProfile = await userProfileRepository.GetFirstOrDefaultAsync(
                x => x.UserId == request.UserId); // UserId = Auth Service User ID

            if (userProfile == null)
            {
                _logger.LogWarning(
                    "UserProfile not found for Auth UserId {UserId}. Cannot create activity log.",
                    request.UserId);

                return Result.Failure(
                    $"UserProfile not found for UserId {request.UserId}",
                    ErrorCodeEnum.NotFound);
            }

            // 2. Create activity log with UserProfile.Id (FK)
            var activityLog = new UserActivityLog
            {
                UserId = userProfile.Id, // ✅ Use UserProfile.Id (Primary Key) as FK
                ActivityType = request.ActivityType,
                Description = request.Description,
                Metadata = request.Metadata,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                OccurredAt = DateTime.UtcNow
            };

            // Initialize base entity fields
            activityLog.InitializeEntity();

            // Add to repository
            await activityLogRepository.AddAsync(activityLog);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User activity log created successfully. LogId: {LogId}, UserProfileId: {UserProfileId}, AuthUserId: {AuthUserId}",
                activityLog.Id,
                userProfile.Id,
                request.UserId);

            return Result.Success("User activity log created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating user activity log for User {UserId}, Activity: {ActivityType}",
                request.UserId,
                request.ActivityType);

            return Result.Failure(
                "Failed to create user activity log",
                ErrorCodeEnum.InternalError);
        }
    }
}
