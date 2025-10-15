using SharedLibrary.Commons.EventBus;
using SharedLibrary.Commons.Enums;
using System.Text.Json.Serialization;

namespace SharedLibrary.Contracts.User.Events;

/// <summary>
/// Event when a new user is created (separate from registration saga)
/// </summary>
public record UserCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; init; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = new();

    [JsonPropertyName("status")]
    public EntityStatusEnum Status { get; init; } = EntityStatusEnum.Active;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("created_by")]
    public Guid? CreatedBy { get; init; }

    [JsonPropertyName("is_email_verified")]
    public bool IsEmailVerified { get; init; }

    [JsonPropertyName("profile_data")]
    public Dictionary<string, object>? ProfileData { get; init; }

    public UserCreatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when user information is updated
/// </summary>
public record UserUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; init; }

    [JsonPropertyName("old_email")]
    public string? OldEmail { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("changed_fields")]
    public List<string> ChangedFields { get; init; } = new();

    [JsonPropertyName("profile_data")]
    public Dictionary<string, object>? ProfileData { get; init; }

    public UserUpdatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when user is deleted (soft or hard delete)
/// </summary>
public record UserDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deletion_type")]
    public string DeletionType { get; init; } = "SoftDelete"; // SoftDelete, HardDelete, GDPR

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("is_permanent")]
    public bool IsPermanent { get; init; } = false;

    [JsonPropertyName("data_retention_policy")]
    public Dictionary<string, object>? DataRetentionPolicy { get; init; }

    public UserDeletedEvent() : base("UserService") { }
}

/// <summary>
/// Event when user profile is activated/deactivated
/// </summary>
public record UserActivationChangedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("old_status")]
    public EntityStatusEnum OldStatus { get; init; }

    [JsonPropertyName("new_status")]
    public EntityStatusEnum NewStatus { get; init; }

    [JsonPropertyName("changed_at")]
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("changed_by")]
    public Guid ChangedBy { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    public UserActivationChangedEvent() : base("UserService") { }
}

/// <summary>
/// Event when user email is verified
/// </summary>
public record UserEmailVerifiedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("verified_at")]
    public DateTime VerifiedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("verification_method")]
    public string VerificationMethod { get; init; } = "Email"; // Email, SMS, Manual

    public UserEmailVerifiedEvent() : base("UserService") { }
}
