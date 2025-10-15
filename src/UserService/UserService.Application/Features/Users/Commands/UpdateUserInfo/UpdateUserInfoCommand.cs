using MediatR;
using SharedLibrary.Commons.Models;
using UserService.Application.Commons.DTOs;

namespace UserService.Application.Features.Users.Commands.UpdateUserInfo;

/// <summary>
/// Command to update user information
/// Email/Phone changes trigger RPC sync with AuthService
/// </summary>
public record UpdateUserInfoCommand : IRequest<Result<UserProfileResponse>>
{
    /// <summary>
    /// Auth Service User ID (AppUser.Id) - NOT UserProfile.Id
    /// Used for: Cache key, UserProfile query (FK), AuthService RPC
    /// </summary>
    public Guid UserId { get; init; }
    
    public UpdateUserInfoRequest Request { get; init; } = null!;
}
