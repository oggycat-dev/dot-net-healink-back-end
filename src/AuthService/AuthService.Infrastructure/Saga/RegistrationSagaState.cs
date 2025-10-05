using MassTransit;
using SharedLibrary.Commons.Enums;

namespace AuthService.Infrastructure.Saga;

/// <summary>
/// Registration Saga State - Owned by AuthService
/// Each saga instance represents ONE registration attempt with unique CorrelationId
/// </summary>
public class RegistrationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    
    // User Information
    public string Email { get; set; } = string.Empty;
    public string? EncryptedPassword { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    
    // OTP Information
    public string? OtpCode { get; set; }
    public NotificationChannelEnum Channel { get; set; }
    public int ExpiresInMinutes { get; set; }
    
    // Process Tracking
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? OtpSentAt { get; set; }
    public DateTime? OtpVerifiedAt { get; set; }
    public DateTime? AuthUserCreatedAt { get; set; }
    public DateTime? UserProfileCreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Error Handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    
    // Scheduling
    public Guid? OtpTimeoutTokenId { get; set; }
    
    // Result
    public Guid? AuthUserId { get; set; } // ID của AppUser trong AuthService
    public Guid? UserProfileId { get; set; } // ID của UserProfile trong UserService
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
}
