using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.User.Events;
using UserService.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UserService.Application.Features.CreatorApplications.Commands;

public class SubmitCreatorApplicationHandler : IRequestHandler<SubmitCreatorApplicationCommand, SubmitCreatorApplicationResponse>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SubmitCreatorApplicationHandler> _logger;

    public SubmitCreatorApplicationHandler(
        IOutboxUnitOfWork unitOfWork,
        IEventBus eventBus,
        ILogger<SubmitCreatorApplicationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<SubmitCreatorApplicationResponse> Handle(SubmitCreatorApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing creator application for user: {UserId}", request.UserId);

            // Check if user already has pending application
            var existingApplication = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == request.UserId && 
                                         x.ApplicationStatus == ApplicationStatusEnum.Pending, cancellationToken);

            if (existingApplication != null)
            {
                return new SubmitCreatorApplicationResponse
                {
                    Success = false,
                    ApplicationId = existingApplication.Id,
                    Status = existingApplication.ApplicationStatus.ToString(),
                    SubmittedAt = existingApplication.SubmittedAt,
                    Message = "Bạn đã có một đơn đăng ký đang chờ duyệt. Vui lòng đợi admin xem xét."
                };
            }

            // Get user profile
            var userProfile = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (userProfile == null)
            {
                throw new InvalidOperationException($"Không tìm thấy profile người dùng với UserId: {request.UserId}");
            }

            // Find ContentCreator role
            var contentCreatorRole = await _unitOfWork.Repository<BusinessRole>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.RoleType == BusinessRoleEnum.ContentCreator, cancellationToken);

            if (contentCreatorRole == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò Content Creator trong hệ thống");
            }

            // Create application data
            var applicationData = new Dictionary<string, object>
            {
                ["experience"] = request.Experience,
                ["portfolio"] = request.Portfolio ?? string.Empty,
                ["motivation"] = request.Motivation,
                ["social_media"] = request.SocialMedia ?? new Dictionary<string, string>(),
                ["additional_info"] = request.AdditionalInfo ?? string.Empty
            };

            // Create creator application
            var creatorApplication = new CreatorApplication
            {
                UserId = userProfile.Id, // Use UserProfile.Id for foreign key constraint
                ApplicationData = JsonSerializer.Serialize(applicationData),
                ApplicationStatus = ApplicationStatusEnum.Pending,
                SubmittedAt = DateTime.UtcNow,
                RequestedBusinessRoleId = contentCreatorRole.Id
            };

            await _unitOfWork.Repository<CreatorApplication>().AddAsync(creatorApplication);
            
            // Commit transaction first
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Creator application saved to database. ApplicationId: {ApplicationId}, UserId: {UserId}", 
                creatorApplication.Id, creatorApplication.UserId);

            // Publish event after successful commit
            var applicationEvent = new CreatorApplicationSubmittedEvent
            {
                ApplicationId = creatorApplication.Id,
                UserId = request.UserId,
                UserEmail = userProfile.Email,
                UserName = userProfile.FullName,
                SubmittedAt = creatorApplication.SubmittedAt,
                ApplicationData = applicationData
            };

            await _eventBus.PublishAsync(applicationEvent);
            
            _logger.LogInformation("Creator application event published successfully. ApplicationId: {ApplicationId}", 
                creatorApplication.Id);

            _logger.LogInformation("Creator application submitted successfully. ApplicationId: {ApplicationId}", 
                creatorApplication.Id);

            return new SubmitCreatorApplicationResponse
            {
                Success = true,
                ApplicationId = creatorApplication.Id,
                Status = creatorApplication.ApplicationStatus.ToString(),
                SubmittedAt = creatorApplication.SubmittedAt,
                Message = "Đơn đăng ký làm Content Creator đã được nộp thành công. Chúng tôi sẽ xem xét trong thời gian sớm nhất!"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting creator application for user: {UserId}", request.UserId);
            throw;
        }
    }
}
