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