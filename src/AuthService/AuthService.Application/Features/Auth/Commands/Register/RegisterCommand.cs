using AuthService.Application.Commons.DTOs;
using MediatR;
using SharedLibrary.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.Register;

public record RegisterCommand(RegisterRequest Request) : IRequest<Result>;