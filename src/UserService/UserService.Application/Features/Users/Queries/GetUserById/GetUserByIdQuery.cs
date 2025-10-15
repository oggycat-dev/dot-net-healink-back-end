using MediatR;
using SharedLibrary.Commons.Models;
using UserService.Application.Commons.DTOs;

namespace UserService.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Query to get single user profile by ID with roles from AuthService
/// </summary>
public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserProfileResponse>>;
