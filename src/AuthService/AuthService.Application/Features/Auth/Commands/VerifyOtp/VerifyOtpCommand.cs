using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using MediatR;

namespace AuthService.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpRequest
{
    public string Contact { get; init; } = string.Empty;
    public string OtpCode { get; init; } = string.Empty;
    public NotificationChannelEnum Channel { get; init; } = NotificationChannelEnum.Email;
}

public record VerifyOtpCommand(VerifyOtpRequest Request) : IRequest<Result>;
