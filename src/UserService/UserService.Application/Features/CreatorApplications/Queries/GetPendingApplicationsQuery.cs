using MediatR;
using SharedLibrary.Commons.Enums;

namespace UserService.Application.Features.CreatorApplications.Queries;

/// <summary>
/// Query để lấy danh sách đơn đăng ký đang chờ duyệt
/// </summary>
public record GetPendingApplicationsQuery : IRequest<List<PendingApplicationDto>>
{
    public int PageSize { get; init; } = 10;
    public int PageNumber { get; init; } = 1;
}

public record PendingApplicationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid AuthUserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string UserFullName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public ApplicationStatusEnum Status { get; init; }
    public string ExperienceSummary { get; init; } = string.Empty;
    public string PortfolioUrl { get; init; } = string.Empty;
    public string BusinessRoleName { get; init; } = string.Empty;
}
