namespace AuthService.Application.Commons.DTOs;

/// <summary>
/// Response DTO for user state information
/// </summary>
public record UserStateResponse
{
    public Guid UserId { get; init; }
    public Guid UserProfileId { get; init; }
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime LastLoginAt { get; init; }
    public DateTime CacheUpdatedAt { get; init; }
    
    // Subscription Info
    public UserSubscriptionResponse? Subscription { get; init; }
    
    // Computed Properties
    public bool IsActive { get; init; }
    public bool HasActiveSubscription { get; init; }
    public bool IsContentCreator { get; init; }
}

/// <summary>
/// Response DTO for user subscription information
/// </summary>
public record UserSubscriptionResponse
{
    public Guid SubscriptionId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public string SubscriptionPlanDisplayName { get; init; } = string.Empty;
    public int SubscriptionStatus { get; init; }
    public string SubscriptionStatusName { get; init; } = string.Empty;
    public DateTime? CurrentPeriodStart { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? CanceledAt { get; init; }
    
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
}

/// <summary>
/// Response DTO for content creator status check
/// </summary>
public record ContentCreatorStatusResponse
{
    public bool IsContentCreator { get; init; }
    public string? Reason { get; init; }
    public DateTime CheckedAt { get; init; }
    public string Source { get; init; } = string.Empty; // "cache" or "identity_service"
}
