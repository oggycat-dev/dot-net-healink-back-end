using MassTransit;
using SharedLibrary.Commons.Enums;

namespace AuthService.Infrastructure.Saga;

/// <summary>
/// Admin User Creation Saga State - Owned by AuthService (like RegistrationSaga)
/// Each saga instance represents ONE admin-initiated user creation with unique CorrelationId
/// Pattern: Same as RegistrationSaga for consistency
/// </summary>
public class AdminUserCreationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    
    // User Information
    public string Email { get; set; } = string.Empty;
    public string? EncryptedPassword { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public RoleEnum Role { get; set; } = RoleEnum.User;
    
    // Process Tracking
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? AuthUserCreatedAt { get; set; }
    public DateTime? UserProfileUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Creator Information
    public Guid? CreatedBy { get; set; } // Admin/Staff who initiated creation
    
    // Pre-created UserProfile (follows Register pattern)
    public Guid? UserProfileId { get; set; } // Pre-created UserProfile ID
    
    // Error Handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    
    // Result
    public Guid? AuthUserId { get; set; } // ID of AppUser in AuthService
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
}
