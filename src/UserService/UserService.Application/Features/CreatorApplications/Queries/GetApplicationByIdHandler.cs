using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Outbox;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UserService.Application.Features.CreatorApplications.Queries;

public class GetApplicationByIdHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDetailDto?>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<GetApplicationByIdHandler> _logger;

    public GetApplicationByIdHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<GetApplicationByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationDetailDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching creator application details for ID: {ApplicationId}", request.ApplicationId);

            var application = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .Include(x => x.User)
                .Include(x => x.ReviewedByUser)
                .Include(x => x.RequestedBusinessRole)
                .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken);

            if (application == null)
            {
                _logger.LogWarning("Application with ID {ApplicationId} not found", request.ApplicationId);
                return null;
            }

            // Parse application data
            var experience = string.Empty;
            var portfolio = string.Empty;
            var motivation = string.Empty;
            var additionalInfo = string.Empty;
            var socialMedia = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(application.ApplicationData))
            {
                try
                {
                    var appData = JsonSerializer.Deserialize<Dictionary<string, object>>(application.ApplicationData);
                    if (appData != null)
                    {
                        if (appData.ContainsKey("experience"))
                            experience = appData["experience"]?.ToString() ?? string.Empty;
                            
                        if (appData.ContainsKey("portfolio"))
                            portfolio = appData["portfolio"]?.ToString() ?? string.Empty;
                            
                        if (appData.ContainsKey("motivation"))
                            motivation = appData["motivation"]?.ToString() ?? string.Empty;
                            
                        if (appData.ContainsKey("additional_info"))
                            additionalInfo = appData["additional_info"]?.ToString() ?? string.Empty;
                            
                        if (appData.ContainsKey("social_media") && appData["social_media"] != null)
                        {
                            var socialMediaJson = appData["social_media"].ToString();
                            if (!string.IsNullOrEmpty(socialMediaJson))
                            {
                                var socialMediaDict = JsonSerializer.Deserialize<Dictionary<string, string>>(socialMediaJson);
                                if (socialMediaDict != null)
                                {
                                    socialMedia = socialMediaDict;
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing application data JSON for application: {ApplicationId}", application.Id);
                }
            }

            var result = new ApplicationDetailDto
            {
                Id = application.Id,
                UserId = application.UserId,
                AuthUserId = application.User?.UserId ?? Guid.Empty,
                UserEmail = application.User?.Email ?? "unknown@email.com",
                UserFullName = application.User?.FullName ?? "Unknown User",
                SubmittedAt = application.SubmittedAt,
                Status = application.ApplicationStatus,
                Experience = experience,
                Portfolio = portfolio,
                Motivation = motivation,
                AdditionalInfo = additionalInfo,
                SocialMedia = socialMedia,
                ReviewedAt = application.ReviewedAt,
                ReviewedByName = application.ReviewedByUser?.FullName,
                RejectionReason = application.RejectionReason,
                ReviewNotes = application.ReviewNotes,
                BusinessRoleName = application.RequestedBusinessRole?.Name ?? "Content Creator"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching creator application details for ID: {ApplicationId}", request.ApplicationId);
            throw;
        }
    }
}
