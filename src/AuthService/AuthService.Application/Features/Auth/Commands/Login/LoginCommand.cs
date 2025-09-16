using MediatR;
using AuthService.Application.Commons.DTOs;
using ProductAuthMicroservice.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;