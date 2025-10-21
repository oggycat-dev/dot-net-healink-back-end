using MediatR;
using UserService.Application.Features.CreatorApplications.Queries;
using SharedLibrary.Commons.Outbox;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using SharedLibrary.Commons.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace UserService.Application.Features.CreatorApplications.Handlers;

/// <summary>
/// Handler để lấy trạng thái đơn đăng ký Content Creator của user hiện tại
/// </summary>
public class GetMyApplicationStatusHandler : IRequestHandler<GetMyApplicationStatusQuery, MyApplicationStatusDto?>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<GetMyApplicationStatusHandler> _logger;

    public GetMyApplicationStatusHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<GetMyApplicationStatusHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MyApplicationStatusDto?> Handle(GetMyApplicationStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting application status for user: {UserId}", request.UserId);

            // First, get UserProfile to get the correct UserId for foreign key lookup
            var userProfile = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (userProfile == null)
            {
                _logger.LogWarning("UserProfile not found for UserId: {UserId}", request.UserId);
                return null;
            }

            _logger.LogInformation("Found UserProfile with Id: {UserProfileId} for UserId: {UserId}", 
                userProfile.Id, request.UserId);

            // First, check if there are any applications at all
            var totalApplications = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .CountAsync(cancellationToken);
            
            _logger.LogInformation("Total applications in database: {TotalApplications}", totalApplications);

            // Get applications for this specific user using UserProfile.Id
            // Force fresh data by using AsNoTracking and explicit ordering
                var userApplications = await _unitOfWork.Repository<CreatorApplication>()
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(a => a.UserId == userProfile.Id)
                    .OrderByDescending(a => a.SubmittedAt)
                    .ThenByDescending(a => a.ReviewedAt) // Add ReviewedAt to ensure latest data
                    .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Applications found for user {UserId}: {Count}", request.UserId, userApplications.Count);
            
            // Log all applications for debugging
            foreach (var app in userApplications.Take(3))
            {
                _logger.LogInformation("Application: ID={ApplicationId}, Status={Status}, SubmittedAt={SubmittedAt}, ReviewedAt={ReviewedAt}, RejectionReason={RejectionReason}", 
                    app.Id, app.ApplicationStatus, app.SubmittedAt, app.ReviewedAt, app.RejectionReason);
            }

            if (userApplications.Count == 0)
            {
                // Log all applications to debug
                var allApplications = await _unitOfWork.Repository<CreatorApplication>()
                    .GetQueryable()
                    .OrderByDescending(a => a.SubmittedAt)
                    .Take(5)
                    .Select(a => new { a.Id, a.UserId, a.SubmittedAt })
                    .ToListAsync(cancellationToken);
                
                _logger.LogInformation("Recent applications in database: {Applications}", 
                    string.Join(", ", allApplications.Select(a => $"ID:{a.Id}, UserId:{a.UserId}, SubmittedAt:{a.SubmittedAt}")));

                _logger.LogInformation("No application found for user: {UserId}", request.UserId);
                return null;
            }

            var application = userApplications.First();

            _logger.LogInformation("Retrieved application from database: ID={ApplicationId}, Status={Status}, ReviewedAt={ReviewedAt}, RejectionReason={RejectionReason}", 
                application.Id, application.ApplicationStatus, application.ReviewedAt, application.RejectionReason);

            // Parse application data from JSON
            var applicationData = JsonSerializer.Deserialize<Dictionary<string, object>>(application.ApplicationData) ?? new Dictionary<string, object>();

            var result = new MyApplicationStatusDto
            {
                ApplicationId = application.Id,
                Status = application.ApplicationStatus.ToString(),
                StatusDescription = GetStatusDescription(application.ApplicationStatus),
                SubmittedAt = application.SubmittedAt,
                ReviewedAt = application.ReviewedAt,
                ReviewedBy = application.ReviewedBy,
                RejectionReason = application.RejectionReason,
                Experience = applicationData.GetValueOrDefault("experience")?.ToString() ?? "",
                Portfolio = applicationData.GetValueOrDefault("portfolio")?.ToString() ?? "",
                Motivation = applicationData.GetValueOrDefault("motivation")?.ToString() ?? "",
                SocialMedia = applicationData.GetValueOrDefault("socialMedia")?.ToString()?.Split(',').ToList() ?? new List<string>(),
                AdditionalInfo = applicationData.GetValueOrDefault("additionalInfo")?.ToString(),
                RequestedBusinessRole = applicationData.GetValueOrDefault("requestedBusinessRole")?.ToString() ?? "Content Creator",
                CanResubmit = CanResubmit(application.ApplicationStatus),
                NextSteps = GetNextSteps(application.ApplicationStatus)
            };

            _logger.LogInformation("Application status retrieved for user: {UserId}, Status: {Status}", 
                request.UserId, application.ApplicationStatus);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application status for user: {UserId}", request.UserId);
            throw;
        }
    }

    private static string GetStatusDescription(ApplicationStatusEnum status)
    {
        return status switch
        {
            ApplicationStatusEnum.Pending => "Đơn đăng ký đang chờ duyệt",
            ApplicationStatusEnum.Approved => "Đơn đăng ký đã được phê duyệt",
            ApplicationStatusEnum.Rejected => "Đơn đăng ký đã bị từ chối",
            _ => "Trạng thái không xác định"
        };
    }

    private static bool CanResubmit(ApplicationStatusEnum status)
    {
        return status == ApplicationStatusEnum.Rejected;
    }

    private static string GetNextSteps(ApplicationStatusEnum status)
    {
        return status switch
        {
            ApplicationStatusEnum.Pending => "Vui lòng chờ admin duyệt đơn đăng ký của bạn",
            ApplicationStatusEnum.Approved => "Chúc mừng! Bạn đã trở thành Content Creator. Bạn có thể bắt đầu tạo nội dung.",
            ApplicationStatusEnum.Rejected => "Đơn đăng ký của bạn đã bị từ chối. Bạn có thể nộp đơn mới sau khi cải thiện hồ sơ.",
            _ => "Liên hệ admin để được hỗ trợ"
        };
    }
}
