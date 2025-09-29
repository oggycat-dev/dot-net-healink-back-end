using AuthService.Application.Commons.DTOs;
using MediatR;
using SharedLibrary.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand() : IRequest<Result<AuthResponse>>;