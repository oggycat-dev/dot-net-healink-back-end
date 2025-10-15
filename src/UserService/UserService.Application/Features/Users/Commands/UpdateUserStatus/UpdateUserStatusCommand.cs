using MediatR;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;

namespace UserService.Application.Features.Users.Commands.UpdateUserStatus;

/// <summary>
/// Command to update user status
/// Updates both UserService and publishes event to AuthService
/// Syncs to cache immediately
/// </summary>
public record UpdateUserStatusCommand : IRequest<Result>
{
    /// <summary>
    /// Auth Service User ID (AppUser.Id) - NOT UserProfile.Id
    /// Used for: Cache key, UserProfile query (FK), Event publishing
    /// </summary>
    public Guid UserId { get; init; }
    
    public EntityStatusEnum Status { get; init; }
    public string? Reason { get; init; }
}
