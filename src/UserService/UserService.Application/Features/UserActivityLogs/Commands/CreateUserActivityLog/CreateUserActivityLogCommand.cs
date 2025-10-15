using MediatR;
using SharedLibrary.Commons.Models;

namespace UserService.Application.Features.UserActivityLogs.Commands.CreateUserActivityLog;

/// <summary>
/// Command để ghi log hoạt động của user
/// </summary>
public record CreateUserActivityLogCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Metadata { get; init; } = "{}";
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
