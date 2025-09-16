using ProductAuthMicroservice.Commons.EventBus;
using ProductAuthMicroservice.Commons.Enums;
using System.Text.Json.Serialization;

namespace ProductAuthMicroservice.Shared.Contracts.Events;

#region User Authentication Events

/// <summary>
/// Event when user logs in
/// </summary>
public record UserLoggedInEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<string> Roles { get; init; } = new();

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token_expiry")]
    public DateTime RefreshTokenExpiryTime { get; init; }

    [JsonPropertyName("login_at")]
    public DateTime LoginAt { get; init; }

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; init; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; init; }

    public UserLoggedInEvent() : base("AuthService") { }
}

/// <summary>
/// Event when user logs out
/// </summary>
public record UserLoggedOutEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("logout_at")]
    public DateTime LogoutAt { get; init; }

    [JsonPropertyName("logout_type")]
    public string LogoutType { get; init; } = "Manual"; // Manual, Force, TokenExpired

    public UserLoggedOutEvent() : base("AuthService") { }
}

/// <summary>
/// Event when refresh token is revoked
/// </summary>
public record RefreshTokenRevokedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("revoked_at")]
    public DateTime RevokedAt { get; init; }

    [JsonPropertyName("revoked_by")]
    public Guid? RevokedBy { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    public RefreshTokenRevokedEvent() : base("AuthService") { }
}

#endregion

#region User Status Events

/// <summary>
/// Event when user status changes (Active, Inactive, Locked, etc.)
/// </summary>
public record UserStatusChangedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("old_status")]
    public EntityStatusEnum OldStatus { get; init; }

    [JsonPropertyName("new_status")]
    public EntityStatusEnum NewStatus { get; init; }

    [JsonPropertyName("changed_by")]
    public Guid ChangedBy { get; init; }

    [JsonPropertyName("changed_at")]
    public DateTime ChangedAt { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    public UserStatusChangedEvent() : base("AuthService") { }
}

/// <summary>
/// Event when user is locked/unlocked
/// </summary>
public record UserLockStatusChangedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("is_locked")]
    public bool IsLocked { get; init; }

    [JsonPropertyName("lock_reason")]
    public string? LockReason { get; init; }

    [JsonPropertyName("locked_until")]
    public DateTime? LockedUntil { get; init; }

    [JsonPropertyName("changed_by")]
    public Guid ChangedBy { get; init; }

    [JsonPropertyName("changed_at")]
    public DateTime ChangedAt { get; init; }

    public UserLockStatusChangedEvent() : base("AuthService") { }
}

#endregion

#region User Role Events

/// <summary>
/// Event when user roles are changed
/// </summary>
public record UserRolesChangedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("old_roles")]
    public List<string> OldRoles { get; init; } = new();

    [JsonPropertyName("new_roles")]
    public List<string> NewRoles { get; init; } = new();

    [JsonPropertyName("added_roles")]
    public List<string> AddedRoles { get; init; } = new();

    [JsonPropertyName("removed_roles")]
    public List<string> RemovedRoles { get; init; } = new();

    [JsonPropertyName("changed_by")]
    public Guid ChangedBy { get; init; }

    [JsonPropertyName("changed_at")]
    public DateTime ChangedAt { get; init; }

    public UserRolesChangedEvent() : base("AuthService") { }
}

/// <summary>
/// Event when role is added to user
/// </summary>
public record RoleAddedToUserEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("role_name")]
    public string RoleName { get; init; } = string.Empty;

    [JsonPropertyName("added_by")]
    public Guid AddedBy { get; init; }

    [JsonPropertyName("added_at")]
    public DateTime AddedAt { get; init; }

    public RoleAddedToUserEvent() : base("AuthService") { }
}

/// <summary>
/// Event when role is removed from user
/// </summary>
public record RoleRemovedFromUserEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("role_name")]
    public string RoleName { get; init; } = string.Empty;

    [JsonPropertyName("removed_by")]
    public Guid RemovedBy { get; init; }

    [JsonPropertyName("removed_at")]
    public DateTime RemovedAt { get; init; }

    public RoleRemovedFromUserEvent() : base("AuthService") { }
}

#endregion

#region Session Events

/// <summary>
/// Event for session invalidation (force logout all sessions)
/// </summary>
public record UserSessionsInvalidatedEvent : IntegrationEvent
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("invalidated_by")]
    public Guid InvalidatedBy { get; init; }

    [JsonPropertyName("invalidated_at")]
    public DateTime InvalidatedAt { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    public UserSessionsInvalidatedEvent() : base("AuthService") { }
}

#endregion
