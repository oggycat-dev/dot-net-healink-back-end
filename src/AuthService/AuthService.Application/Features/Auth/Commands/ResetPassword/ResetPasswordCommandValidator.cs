using System.Text.RegularExpressions;
using FluentValidation;
using SharedLibrary.Commons.Enums;

namespace AuthService.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Request.Contact)
            .NotEmpty().WithMessage("Contact is required")
            .MaximumLength(100).WithMessage("Contact must not exceed 100 characters")
            .Must((command, contact) => command.Request.OtpSentChannel == NotificationChannelEnum.Email ?
                IsValidEmail(contact) :
                IsValidPhoneNumber(contact))
            .WithMessage("Invalid contact format");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long")
            .MaximumLength(100).WithMessage("New password must not exceed 100 characters");

        RuleFor(x => x.Request.OtpSentChannel)
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