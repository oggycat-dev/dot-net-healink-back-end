using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Rpc;

/// <summary>
/// RPC Request to sync user roles to AuthService
/// Pattern: Request-Response with rollback capability
/// Timeout: 5 seconds
/// </summary>
public record UpdateUserRolesRpcRequest : IntegrationEvent
{
    public Guid UserId { get; init; }
    public List<string> OldRoles { get; init; } = new();
    public List<string> NewRoles { get; init; } = new();
    public List<string> AddedRoles { get; init; } = new();
    public List<string> RemovedRoles { get; init; } = new();
    public Guid UpdatedBy { get; init; }
}

/// <summary>
/// RPC Response from AuthService after role update
/// </summary>
public record UpdateUserRolesRpcResponse : IntegrationEvent
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid UserId { get; init; }
    public List<string> UpdatedRoles { get; init; } = new();
}
