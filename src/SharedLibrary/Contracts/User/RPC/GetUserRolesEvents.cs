using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.User.Rpc;

/// <summary>
/// RPC Request to get roles for multiple users from AuthService
/// Pattern: Request-Response with Dictionary result for efficient lookup
/// </summary>
public record GetUserRolesRequest : IntegrationEvent
{
    /// <summary>
    /// List of UserIds to get roles for
    /// Size should match page size for optimal performance
    /// </summary>
    public List<Guid> UserIds { get; init; } = new();
    
    public GetUserRolesRequest() : base("UserService") { }
}

/// <summary>
/// RPC Response with roles mapped by UserId
/// Dictionary enables O(1) lookup when mapping to UserProfileResponse
/// </summary>
public record GetUserRolesResponse : IntegrationEvent
{
    /// <summary>
    /// Dictionary mapping UserId to list of role names
    /// Key: UserId (Guid)
    /// Value: List of role names (e.g., ["Admin", "Staff"])
    /// </summary>
    public Dictionary<Guid, List<string>> UserRoles { get; init; } = new();
    
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    
    public GetUserRolesResponse() : base("AuthService") { }
}
