using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Outbox;
using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UserService.Application.Features.CreatorApplications.Queries;

public class GetPendingApplicationsHandler : IRequestHandler<GetPendingApplicationsQuery, List<PendingApplicationDto>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly ILogger<GetPendingApplicationsHandler> _logger;

    public GetPendingApplicationsHandler(
        IOutboxUnitOfWork unitOfWork,
        ILogger<GetPendingApplicationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<PendingApplicationDto>> Handle(GetPendingApplicationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching pending creator applications, page: {PageNumber}, size: {PageSize}", 
                request.PageNumber, request.PageSize);

            var skipAmount = (request.PageNumber - 1) * request.PageSize;

            var applications = await _unitOfWork.Repository<CreatorApplication>()
                .GetQueryable()
                .Include(x => x.User)
                .Include(x => x.RequestedBusinessRole)
                .Where(x => x.ApplicationStatus == ApplicationStatusEnum.Pending)
                .OrderByDescending(x => x.SubmittedAt)
                .Skip(skipAmount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = new List<PendingApplicationDto>();

            foreach (var app in applications)
            {
                string experienceSummary = string.Empty;
                string portfolioUrl = string.Empty;
                
                if (!string.IsNullOrEmpty(app.ApplicationData))
                {
                    try
                    {
                        var appData = JsonSerializer.Deserialize<Dictionary<string, object>>(app.ApplicationData);
                        if (appData != null)
                        {
                            if (appData.ContainsKey("experience"))
                            {
                                var experience = appData["experience"]?.ToString() ?? string.Empty;
                                experienceSummary = experience.Length > 100 ? 
                                    experience.Substring(0, 97) + "..." : 
                                    experience;
                            }
                            
                            if (appData.ContainsKey("portfolio"))
                            {
                                portfolioUrl = appData["portfolio"]?.ToString() ?? string.Empty;
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error parsing application data JSON for application: {ApplicationId}", app.Id);
                    }
                }

                result.Add(new PendingApplicationDto
                {
                    Id = app.Id,
                    UserId = app.UserId,
                    AuthUserId = app.User?.UserId ?? Guid.Empty,
                    UserEmail = app.User?.Email ?? "unknown@email.com",
                    UserFullName = app.User?.FullName ?? "Unknown User",
                    SubmittedAt = app.SubmittedAt,
                    Status = app.ApplicationStatus,
                    ExperienceSummary = experienceSummary,
                    PortfolioUrl = portfolioUrl,
                    BusinessRoleName = app.RequestedBusinessRole?.Name ?? "Content Creator"
                });
            }

            _logger.LogInformation("Found {Count} pending applications", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending creator applications");
            throw;
        }
    }
}
