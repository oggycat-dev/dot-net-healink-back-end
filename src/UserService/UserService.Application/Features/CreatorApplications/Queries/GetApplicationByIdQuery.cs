using MediatR;
using SharedLibrary.Commons.Enums;
using System.Text.Json;

namespace UserService.Application.Features.CreatorApplications.Queries;

/// <summary>
/// Query để lấy thông tin chi tiết của một đơn đăng ký
/// </summary>
public record GetApplicationByIdQuery : IRequest<ApplicationDetailDto?>
{
    public Guid ApplicationId { get; init; }
}

public record ApplicationDetailDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid AuthUserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string UserFullName { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
    public ApplicationStatusEnum Status { get; init; }
    public string Experience { get; init; } = string.Empty;
    public string Portfolio { get; init; } = string.Empty;
    public string Motivation { get; init; } = string.Empty;
    public Dictionary<string, string> SocialMedia { get; init; } = new();
    public string AdditionalInfo { get; init; } = string.Empty;
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewedByName { get; init; }
    public string? RejectionReason { get; init; }
    public string? ReviewNotes { get; init; }
    public string BusinessRoleName { get; init; } = string.Empty;
}
