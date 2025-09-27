using MediatR;
using System.Text.Json.Serialization;

namespace UserService.Application.Features.CreatorApplications.Commands;

/// <summary>
/// Command để phê duyệt đơn đăng ký làm Content Creator
/// </summary>
public record ApproveCreatorApplicationCommand : IRequest<ApproveCreatorApplicationResponse>
{
    [JsonPropertyName("application_id")]
    public Guid ApplicationId { get; init; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
    
    // Added internally by controller
    public Guid ReviewerId { get; set; }
}

public record ApproveCreatorApplicationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public DateTime ApprovedAt { get; init; }
}
