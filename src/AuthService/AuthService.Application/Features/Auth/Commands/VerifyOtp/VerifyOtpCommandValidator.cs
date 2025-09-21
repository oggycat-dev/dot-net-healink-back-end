using FluentValidation;
using SharedLibrary.Commons.Enums;
using System.Text.RegularExpressions;

namespace AuthService.Application.Features.Auth.Commands.VerifyOtp;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Request.Contact)
            .NotEmpty().WithMessage("Contact is required")
            .MaximumLength(100).WithMessage("Contact must not exceed 100 characters")
            .Must((command, contact) => command.Request.Channel == NotificationChannelEnum.Email ? 
                IsValidEmail(contact) : 
                IsValidPhoneNumber(contact))
            .WithMessage("Invalid contact format");

        RuleFor(x => x.Request.OtpCode)
            .NotEmpty().WithMessage("OTP code is required")
            .Matches("^[0-9]{6}$").WithMessage("OTP code must be 6 digits");

        RuleFor(x => x.Request.Channel)
            .IsInEnum().WithMessage("Invalid notification channel");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        return emailRegex.IsMatch(email);
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Vietnamese phone number validation: 0xxxxxxxxx (10 digits), 84xxxxxxxxx, or +84xxxxxxxxx
        var phoneRegex = new Regex(@"^(0[0-9]{9}|84[0-9]{9,10}|\+84[0-9]{9,10})$");
        return phoneRegex.IsMatch(phoneNumber);
    }
}