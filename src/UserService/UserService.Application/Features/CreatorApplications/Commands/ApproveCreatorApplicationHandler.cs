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

            if (application.User == null)
            {
                throw new InvalidOperationException($"Không tìm thấy thông tin user cho đơn đăng ký ID: {request.ApplicationId}");
            }

            _logger.LogInformation("Found application: {ApplicationId}, UserId: {UserId}, UserEmail: {UserEmail}", 
                application.Id, application.User.UserId, application.User.Email);

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
            
            // Validate ReviewerId exists in UserProfiles before setting
            if (request.ReviewerId != Guid.Empty)
            {
                var reviewerExists = await _unitOfWork.Repository<UserProfile>()
                    .GetQueryable()
                    .AnyAsync(x => x.Id == request.ReviewerId, cancellationToken);
                    
                application.ReviewedBy = reviewerExists ? request.ReviewerId : null;
                
                if (!reviewerExists)
                {
                    _logger.LogWarning("ReviewerId {ReviewerId} not found in UserProfiles, setting ReviewedBy to null", request.ReviewerId);
                }
            }
            else
            {
                application.ReviewedBy = null;
            }
            
            application.ReviewNotes = request.Notes;
            
            // Explicitly mark as modified to ensure EF tracks the changes
            _unitOfWork.Repository<CreatorApplication>().Update(application);
            _logger.LogInformation("Marked application as modified in EF context");

            // Check if user already has ContentCreator role
            var existingRole = await _unitOfWork.Repository<UserBusinessRole>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == application.UserId && 
                                         x.BusinessRoleId == contentCreatorRole.Id, cancellationToken);

            if (existingRole == null)
            {
                // Add user to ContentCreator role
                var userBusinessRole = new UserBusinessRole
                {
                    UserId = application.UserId,
                    BusinessRoleId = contentCreatorRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = request.ReviewerId
                };

                await _unitOfWork.Repository<UserBusinessRole>().AddAsync(userBusinessRole);
                _logger.LogInformation("Added ContentCreator role to user: {UserId}", application.UserId);
            }
            else
            {
                _logger.LogInformation("User {UserId} already has ContentCreator role", application.UserId);
            }

            // Log activity
            var activityLog = new UserActivityLog
            {
                UserId = application.UserId,
                ActivityType = "RoleAssigned",
                Description = $"User assigned to Content Creator role via application approval",
                Metadata = $"{{\"role\":\"ContentCreator\",\"assignedBy\":\"{request.ReviewerId}\"}}",
                IpAddress = "internal",
                OccurredAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserActivityLog>().AddAsync(activityLog);
            
            // Add events to outbox for reliable publishing
            var approvedEvent = new CreatorApplicationApprovedEvent
            {
                ApplicationId = application.Id,
                UserId = application.User.UserId,  // AuthUser ID from UserProfile
                UserEmail = application.User.Email,
                ReviewerId = request.ReviewerId,
                ApprovedAt = application.ReviewedAt.Value,
                BusinessRoleId = contentCreatorRole.Id,
                BusinessRoleName = "ContentCreator"
            };

            await _unitOfWork.AddOutboxEventAsync(approvedEvent);
            _logger.LogInformation("CreatorApplicationApprovedEvent added to outbox. ApplicationId: {ApplicationId}", application.Id);

            // Add system role to AuthUser via event
            var roleAddEvent = new RoleAddedToUserEvent
            {
                UserId = application.User.UserId,  // AuthUser ID from UserProfile
                Email = application.User.Email,
                RoleName = "ContentCreator",
                AddedBy = request.ReviewerId,
                AddedAt = DateTime.UtcNow
            };

            await _unitOfWork.AddOutboxEventAsync(roleAddEvent);
            _logger.LogInformation("RoleAddedToUserEvent added to outbox. UserId: {UserId}", application.User.UserId);

            // Publish UserRolesChangedEvent to update Redis cache in all services
            // Get all current roles of user
            var currentRoles = await _unitOfWork.Repository<UserBusinessRole>()
                .GetQueryable()
                .Where(x => x.UserId == application.UserId)
                .Select(x => x.BusinessRole.Name)
                .ToListAsync(cancellationToken);

            var rolesChangedEvent = new UserRolesChangedEvent
            {
                UserId = application.User.UserId,
                Email = application.User.Email,
                OldRoles = currentRoles.Where(r => r != "ContentCreator").ToList(),
                NewRoles = currentRoles,
                AddedRoles = new List<string> { "ContentCreator" },
                RemovedRoles = new List<string>(),
                ChangedBy = request.ReviewerId,
                ChangedAt = DateTime.UtcNow
            };

            await _unitOfWork.AddOutboxEventAsync(rolesChangedEvent);
            _logger.LogInformation("UserRolesChangedEvent added to outbox to update Redis cache. UserId: {UserId}, NewRoles: {Roles}", 
                application.User.UserId, string.Join(", ", currentRoles));

            // Save all changes with outbox events atomically
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            
            _logger.LogInformation("Application approved and saved to database with outbox events. ApplicationId: {ApplicationId}, Status: {Status}", 
                application.Id, application.ApplicationStatus);

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
