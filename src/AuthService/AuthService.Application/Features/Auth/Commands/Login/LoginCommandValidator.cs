using FluentValidation;
using AuthService.Application.Commons.Interfaces;

namespace AuthService.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required").MinimumLength(8).WithMessage("Password must be at least 8 characters long");

        RuleFor(x => x.Request.GrantType)
            .IsInEnum()
            .WithMessage("Grant type is required");
    }
}