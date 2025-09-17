using MediatR;
using SharedLibrary.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result>;