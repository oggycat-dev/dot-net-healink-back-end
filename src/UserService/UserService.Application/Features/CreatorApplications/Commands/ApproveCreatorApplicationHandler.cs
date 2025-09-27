using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.SharedLibrary.Contracts.Events;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.Application.Features.CreatorApplications.Commands;

public class ApproveCreatorApplicationHandler : IRequestHandler<ApproveCreatorApplicationCommand, ApproveCreatorApplicationResponse>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ApproveCreatorApplicationHandler> _logger;

    public ApproveCreatorApplicationHandler(
        IOutboxUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<ApproveCreatorApplicationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApproveCreatorApplicationResponse> Handle(ApproveCreatorApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing approval of creator application: {ApplicationId}", request.ApplicationId);

            // Find application
            var application = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken);

            if (application == null)
            {
                throw new InvalidOperationException($"Không tìm thấy đơn đăng ký với ID: {request.ApplicationId}");
            }

            if (application.ApplicationStatus != ApplicationStatusEnum.Pending)
            {
                throw new InvalidOperationException($"Đơn đăng ký này không còn ở trạng thái chờ duyệt. Trạng thái hiện tại: {application.ApplicationStatus}");
            }

            // Find ContentCreator role
            var contentCreatorRole = await _unitOfWork.Repository<BusinessRole>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.RoleType == BusinessRoleEnum.ContentCreator, cancellationToken);

            if (contentCreatorRole == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò Content Creator trong hệ thống");
            }

            // Update application
            application.ApplicationStatus = ApplicationStatusEnum.Approved;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedBy = request.ReviewerId;
            application.ReviewNotes = request.Notes;

            // Add user to ContentCreator role
            var userBusinessRole = new UserBusinessRole
            {
                UserId = application.UserId,
                BusinessRoleId = contentCreatorRole.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = request.ReviewerId
            };

            await _unitOfWork.Repository<UserBusinessRole>().AddAsync(userBusinessRole);

            // Log activity
            var activityLog = new UserActivityLog
            {
                UserId = application.UserId,
                ActivityType = "RoleAssigned",
                Description = $"User assigned to Content Creator role via application approval",
                Data = $"{{\"role\":\"ContentCreator\",\"assignedBy\":\"{request.ReviewerId}\"}}",
                IpAddress = "internal",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserActivityLog>().AddAsync(activityLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish approval event
            var approvedEvent = new CreatorApplicationApprovedEvent
            {
                ApplicationId = application.Id,
                UserId = application.User.UserId,  // AuthUser ID
                UserEmail = application.User.Email,
                ReviewerId = request.ReviewerId,
                ApprovedAt = application.ReviewedAt.Value,
                BusinessRoleId = contentCreatorRole.Id,
                BusinessRoleName = "ContentCreator"
            };

            await _eventBus.PublishAsync(approvedEvent);

            // Add system role to AuthUser via event
            var roleAddEvent = new RoleAddedToUserEvent
            {
                UserId = application.User.UserId,  // AuthUser ID
                Email = application.User.Email,
                RoleName = "ContentCreator",
                AddedBy = request.ReviewerId,
                AddedAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(roleAddEvent);

            _logger.LogInformation("Creator application approved successfully. ApplicationId: {ApplicationId}, UserId: {UserId}", 
                application.Id, application.User.UserId);

            return new ApproveCreatorApplicationResponse
            {
                Success = true,
                ApplicationId = application.Id,
                UserId = application.User.UserId,
                UserEmail = application.User.Email,
                ApprovedAt = application.ReviewedAt.Value,
                Message = "Đã phê duyệt đơn đăng ký Content Creator thành công và cấp quyền cho người dùng"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving creator application: {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
