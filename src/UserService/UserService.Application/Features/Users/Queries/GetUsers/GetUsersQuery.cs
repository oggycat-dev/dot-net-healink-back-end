using MediatR;
using SharedLibrary.Commons.Models;
using UserService.Application.Commons.DTOs;

namespace UserService.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to get paginated user profiles with dynamic filters
/// Roles are fetched from AuthService via RPC using Task.WhenAll
/// </summary>
public record GetUsersQuery(UserProfileFilter Filter) : IRequest<PaginationResult<UserProfileResponse>>;
