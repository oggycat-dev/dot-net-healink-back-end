using MediatR;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using UserService.Application.Commons.DTOs;

namespace UserService.Application.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Command to create user by admin
/// Pre-creates UserProfile (Pending status), then publishes event to start saga
/// Pattern: Follows RegistrationSaga pre-creation pattern
/// </summary>
public record CreateUserByAdminCommand : IRequest<Result<CreateUserByAdminResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Address { get; init; }
    public RoleEnum Role { get; init; } = RoleEnum.User;
}

/// <summary>
/// Response after creating user by admin
/// </summary>
public record CreateUserByAdminResponse
{
    public Guid UserProfileId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public RoleEnum Role { get; init; }
    public string Message { get; init; } = string.Empty;
}
