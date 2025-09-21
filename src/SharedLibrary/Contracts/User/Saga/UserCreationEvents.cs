using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Saga;

/// <summary>
/// Command to create AppUser in AuthService
/// </summary>
public record CreateAuthUser : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string EncryptedPassword { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    
    public CreateAuthUser() : base("RegistrationSaga") { }
}

/// <summary>
/// Response event after AppUser creation in AuthService
/// </summary>
public record AuthUserCreated : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public AuthUserCreated() : base("AuthService") { }
}

/// <summary>
/// Command to create UserProfile in UserService
/// </summary>
public record CreateUserProfile : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; } // AuthUser ID làm foreign key
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    
    public CreateUserProfile() : base("RegistrationSaga") { }
}

/// <summary>
/// Response event after UserProfile creation
/// </summary>
public record UserProfileCreated : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid UserId { get; init; } // Reference to AuthUser
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public UserProfileCreated() : base("UserService") { }
}

/// <summary>
/// Command to send welcome notification
/// </summary>
public record SendWelcomeNotification : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    
    public SendWelcomeNotification() : base("RegistrationSaga") { }
}

/// <summary>
/// Compensating action to delete AuthUser when UserProfile creation fails
/// </summary>
public record DeleteAuthUser : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    
    public DeleteAuthUser() : base("RegistrationSaga") { }
}

/// <summary>
/// Response event after AuthUser deletion (compensating action)
/// </summary>
public record AuthUserDeleted : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
    
    public AuthUserDeleted() : base("AuthService") { }
}

/// <summary>
/// Compensating action to delete UserProfile when necessary
/// </summary>
public record DeleteUserProfile : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    
    public DeleteUserProfile() : base("RegistrationSaga") { }
}

/// <summary>
/// Response event after UserProfile deletion (compensating action)
/// </summary>
public record UserProfileDeleted : IntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid UserId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
    
    public UserProfileDeleted() : base("UserService") { }
}