namespace SharedLibrary.Contracts.User.Requests;

/// <summary>
/// Request to get UserProfile by UserId (from AuthService)
/// Used during login to cache UserProfileId
/// </summary>
public record GetUserProfileByUserIdRequest
{
    public Guid UserId { get; init; }
}

