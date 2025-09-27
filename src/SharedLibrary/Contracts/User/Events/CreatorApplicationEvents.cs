using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Enums;
using System.Text.Json.Serialization;

namespace SharedLibrary.Contracts.User.Events;

/// <summary>
/// Event when a new creator application is submitted
/// </summary>
public record CreatorApplicationSubmittedEvent : IntegrationEvent
{
    [JsonPropertyName("application_id")]
    public Guid ApplicationId { get; init; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("user_email")]
    public string UserEmail { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("submitted_at")]
    public DateTime SubmittedAt { get; init; }

    [JsonPropertyName("application_data")]
    public Dictionary<string, object> ApplicationData { get; init; } = new();

    public CreatorApplicationSubmittedEvent() : base("UserService") { }
}

/// <summary>
/// Event when a creator application is approved
/// </summary>
public record CreatorApplicationApprovedEvent : IntegrationEvent
{
    [JsonPropertyName("application_id")]
    public Guid ApplicationId { get; init; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("user_email")]
    public string UserEmail { get; init; } = string.Empty;

    [JsonPropertyName("reviewer_id")]
    public Guid ReviewerId { get; init; }

    [JsonPropertyName("approved_at")]
    public DateTime ApprovedAt { get; init; }

    [JsonPropertyName("business_role_id")]
    public Guid BusinessRoleId { get; init; }

    [JsonPropertyName("business_role_name")]
    public string BusinessRoleName { get; init; } = string.Empty;

    public CreatorApplicationApprovedEvent() : base("UserService") { }
}

/// <summary>
/// Event when a creator application is rejected
/// </summary>
public record CreatorApplicationRejectedEvent : IntegrationEvent
{
    [JsonPropertyName("application_id")]
    public Guid ApplicationId { get; init; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("user_email")]
    public string UserEmail { get; init; } = string.Empty;

    [JsonPropertyName("reviewer_id")]
    public Guid ReviewerId { get; init; }

    [JsonPropertyName("rejected_at")]
    public DateTime RejectedAt { get; init; }

    [JsonPropertyName("rejection_reason")]
    public string RejectionReason { get; init; } = string.Empty;

    public CreatorApplicationRejectedEvent() : base("UserService") { }
}
