using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Event to start admin-initiated user creation workflow
/// Published by UserService when admin creates a user via API
/// Pattern: Same as RegistrationStarted
/// </summary>
public record AdminUserCreationStarted : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string EncryptedPassword { get; init; } = string.Empty; // Encrypted password
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Address { get; init; }
    public RoleEnum Role { get; init; } = RoleEnum.User; // Use enum, not string
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    
    // ✅ Pre-created UserProfile info (follows Register pattern)
    public Guid? UserProfileId { get; init; } // Pre-created UserProfile ID
    
    public AdminUserCreationStarted() : base("UserService") { }
}

/// <summary>
/// Command to create AuthUser for admin-initiated user creation
/// Published by AdminUserCreationSaga to AuthService
/// </summary>
public record CreateAuthUserByAdmin : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string EncryptedPassword { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public RoleEnum Role { get; init; } = RoleEnum.User;
    
    public CreateAuthUserByAdmin() : base("AdminUserCreationSaga") { }
}

/// <summary>
/// Response event after admin-initiated AuthUser creation
/// Published by AuthService back to AdminUserCreationSaga
/// </summary>
public record AuthUserCreatedByAdmin : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public AuthUserCreatedByAdmin() : base("AuthService") { }
}

/// <summary>
/// Command to update UserProfile's UserId after AuthUser creation
/// Published by AdminUserCreationSaga to UserService
/// Updates pre-created UserProfile from Pending to Active
/// </summary>
public record UpdateUserProfileUserId : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserProfileId { get; init; } // UserProfile to update
    public Guid UserId { get; init; } // Real AuthUserId from AuthService
    
    public UpdateUserProfileUserId() : base("AdminUserCreationSaga") { }
}

/// <summary>
/// Response event after UserProfile UserId update
/// Published by UserService back to AdminUserCreationSaga
/// </summary>
public record UserProfileUpdatedByAdmin : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserProfileId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    
    public UserProfileUpdatedByAdmin() : base("UserService") { }
}

/// <summary>
/// Compensating action to delete AuthUser when admin-initiated creation fails
/// Published by AdminUserCreationSaga to AuthService
/// </summary>
public record DeleteAuthUserByAdmin : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    
    public DeleteAuthUserByAdmin() : base("AdminUserCreationSaga") { }
}

/// <summary>
/// Response after AuthUser deletion compensation for admin-initiated creation
/// Published by AuthService back to AdminUserCreationSaga
/// </summary>
public record AuthUserDeletedByAdmin : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
    
    public AuthUserDeletedByAdmin() : base("AuthService") { }
}
