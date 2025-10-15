using MediatR;
using AuthService.Application.Commons.DTOs;
using SharedLibrary.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;