using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Rpc;

/// <summary>
/// RPC Request to update user status in AuthService
/// Used when UserService updates user status that needs to be synced
/// Pattern: Request-Response with timeout for rollback capability
/// </summary>
public record UpdateUserStatusRpcRequest : IntegrationEvent
{
    /// <summary>
    /// User ID to update (Auth Service User ID)
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// New status
    /// </summary>
    public EntityStatusEnum Status { get; init; }
    
    /// <summary>
    /// Reason for status change (optional)
    /// </summary>
    public string? Reason { get; init; }
    
    /// <summary>
    /// Who is making the update (for audit trail)
    /// </summary>
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// RPC Response after updating user status in AuthService
/// Includes success/failure for rollback decision in UserService
/// </summary>
public record UpdateUserStatusRpcResponse : IntegrationEvent
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
    /// Status that was actually set (for confirmation)
    /// </summary>
    public EntityStatusEnum UpdatedStatus { get; init; }
}
