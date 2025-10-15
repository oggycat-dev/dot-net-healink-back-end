namespace SharedLibrary.Contracts.User.Responses;

/// <summary>
/// Response containing UserProfileId for caching
/// </summary>
public record GetUserProfileByUserIdResponse
{
    public Guid UserProfileId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool Found { get; init; }
}

