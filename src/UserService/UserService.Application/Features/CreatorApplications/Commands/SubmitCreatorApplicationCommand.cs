using MediatR;
using System.Text.Json.Serialization;

namespace UserService.Application.Features.CreatorApplications.Commands;

/// <summary>
/// Command để nộp đơn đăng ký làm Content Creator
/// </summary>
public record SubmitCreatorApplicationCommand : IRequest<SubmitCreatorApplicationResponse>
{
    [JsonPropertyName("experience")]
    public string Experience { get; init; } = string.Empty;

    [JsonPropertyName("portfolio")]
    public string? Portfolio { get; init; }

    [JsonPropertyName("motivation")]
    public string Motivation { get; init; } = string.Empty;

    [JsonPropertyName("social_media")]
    public Dictionary<string, string> SocialMedia { get; init; } = new();

    [JsonPropertyName("additional_info")]
    public string? AdditionalInfo { get; init; }
    
    // Added internally by controller
    public Guid UserId { get; set; }
}

public record SubmitCreatorApplicationResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime SubmittedAt { get; init; }
}
