using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.User.Events;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.Application.Features.CreatorApplications.Commands;

public class RejectCreatorApplicationHandler : IRequestHandler<RejectCreatorApplicationCommand, RejectCreatorApplicationResponse>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RejectCreatorApplicationHandler> _logger;

    public RejectCreatorApplicationHandler(
        IOutboxUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<RejectCreatorApplicationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<RejectCreatorApplicationResponse> Handle(RejectCreatorApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing rejection of creator application: {ApplicationId}, ReviewerId: {ReviewerId}", 
                request.ApplicationId, request.ReviewerId);

            // Flexible ReviewerId validation - similar to ApproveCreatorApplicationHandler
            // Don't throw exception if reviewerId doesn't exist, just log warning

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

            // Update application - similar to ApproveCreatorApplicationHandler
            application.ApplicationStatus = ApplicationStatusEnum.Rejected;
            application.ReviewedAt = DateTime.UtcNow;
            
            // Flexible ReviewerId handling - similar to ApproveCreatorApplicationHandler
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
            application.RejectionReason = request.RejectionReason;

            // Explicitly mark as modified to ensure EF tracks the changes
            _unitOfWork.Repository<CreatorApplication>().Update(application);
            _logger.LogInformation("Marked application as modified in EF context");

            // Log activity
            var activityLog = new UserActivityLog
            {
                UserId = application.UserId,
                ActivityType = "ApplicationRejected",
                Description = $"Creator application rejected: {request.RejectionReason}",
                Metadata = $"{{\"applicationId\":\"{application.Id}\",\"rejectedBy\":\"{request.ReviewerId}\"}}",
                IpAddress = "internal",
                OccurredAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserActivityLog>().AddAsync(activityLog);
            
            // Add rejection event to outbox for reliable publishing - similar to ApproveCreatorApplicationHandler
            var rejectedEvent = new CreatorApplicationRejectedEvent
            {
                ApplicationId = application.Id,
                UserId = application.User.UserId ?? Guid.Empty,  // AuthUser ID (use Empty if null)
                UserEmail = application.User.Email,
                ReviewerId = request.ReviewerId,
                RejectedAt = application.ReviewedAt.Value,
                RejectionReason = application.RejectionReason ?? "Không đáp ứng yêu cầu"
            };

            await _unitOfWork.AddOutboxEventAsync(rejectedEvent);
            _logger.LogInformation("CreatorApplicationRejectedEvent added to outbox. ApplicationId: {ApplicationId}", application.Id);

            // Save all changes with outbox events atomically - similar to ApproveCreatorApplicationHandler
            await _unitOfWork.SaveChangesWithOutboxAsync(cancellationToken);
            
            _logger.LogInformation("Application rejected and saved to database with outbox events. ApplicationId: {ApplicationId}, Status: {Status}", 
                application.Id, application.ApplicationStatus);

            _logger.LogInformation("Creator application rejected successfully. ApplicationId: {ApplicationId}, UserId: {UserId}, Reason: {Reason}", 
                application.Id, application.User.UserId, application.RejectionReason);

            return new RejectCreatorApplicationResponse
            {
                Success = true,
                ApplicationId = application.Id,
                UserId = application.User.UserId ?? Guid.Empty,
                UserEmail = application.User.Email,
                RejectedAt = application.ReviewedAt.Value,
                Message = "Đã từ chối đơn đăng ký Content Creator"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting creator application: {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
