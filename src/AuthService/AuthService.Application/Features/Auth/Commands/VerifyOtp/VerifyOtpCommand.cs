using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using MediatR;
using AuthService.Application.Commons.DTOs;

namespace AuthService.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(VerifyOtpRequest Request) : IRequest<Result>;
