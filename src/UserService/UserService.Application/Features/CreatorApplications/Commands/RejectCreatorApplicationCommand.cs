using MediatR;
using System.Text.Json.Serialization;

namespace UserService.Application.Features.CreatorApplications.Commands;

/// <summary>
/// Command để từ chối đơn đăng ký làm Content Creator
/// </summary>
public record RejectCreatorApplicationCommand : IRequest<RejectCreatorApplicationResponse>
{
    [JsonPropertyName("application_id")]
    public Guid ApplicationId { get; init; }
    
    [JsonPropertyName("rejection_reason")]
    public string RejectionReason { get; init; } = string.Empty;
    
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
    
    // Added internally by controller
    public Guid ReviewerId { get; set; }
}

public record RejectCreatorApplicationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public DateTime RejectedAt { get; init; }
}
