using AuthService.Application.Commons.DTOs;
using MediatR;
using SharedLibrary.Commons.Models;

namespace AuthService.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<Result>;
