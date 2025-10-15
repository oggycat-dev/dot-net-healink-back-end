using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Rpc;

/// <summary>
/// RPC Request to update user email/phone in AuthService
/// Used when UserService updates user info that needs to be synced
/// Pattern: Request-Response with timeout for rollback capability
/// </summary>
public record UpdateUserInfoRpcRequest : IntegrationEvent
{
    /// <summary>
    /// User ID to update (Auth Service User ID)
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// New email (null if not updating)
    /// </summary>
    public string? Email { get; init; }
    
    /// <summary>
    /// New phone number (null if not updating)
    /// </summary>
    public string? PhoneNumber { get; init; }
    
    /// <summary>
    /// Who is making the update (for audit trail)
    /// </summary>
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// RPC Response after updating user info in AuthService
/// Includes success/failure for rollback decision in UserService
/// </summary>
public record UpdateUserInfoRpcResponse : IntegrationEvent
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Error message if failed
    /// UserService will use this to decide rollback
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Updated user ID
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Email that was actually set (for confirmation)
    /// </summary>
    public string? UpdatedEmail { get; init; }
    
    /// <summary>
    /// Phone that was actually set (for confirmation)
    /// </summary>
    public string? UpdatedPhoneNumber { get; init; }
}
