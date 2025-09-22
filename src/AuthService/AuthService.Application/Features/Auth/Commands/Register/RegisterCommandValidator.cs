using FluentValidation;

namespace AuthService.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long");

        RuleFor(x => x.Request.FullName)
            .NotEmpty().WithMessage("Full name is required");

        RuleFor(x => x.Request.PhoneNumber)
            .NotEmpty().When(x => x.Request.OtpSentChannel == SharedLibrary.Commons.Enums.NotificationChannelEnum.SMS)
            .WithMessage("Phone number is required when OTP channel is SMS")
            .Matches(@"^(0[0-9]{9}|84[0-9]{9,10}|\+84[0-9]{9,10})$").When(x => !string.IsNullOrEmpty(x.Request.PhoneNumber))
            .WithMessage("Invalid phone number format. Must be Vietnamese phone number (0xxxxxxxxx, 84xxxxxxxxx, or +84xxxxxxxxx)");

        RuleFor(x => x.Request.OtpSentChannel)
            .IsInEnum().When(x => x.Request.OtpSentChannel.HasValue)
            .WithMessage("Invalid OTP sent channel");
    }
}