using FluentValidation;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Features.PaymentMethods.Commands.CreatePaymentMethod;

public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Request.ProviderName)
            .NotEmpty().WithMessage("Provider name is required")
            .MaximumLength(100).WithMessage("Provider name must not exceed 100 characters");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Request.Type)
            .IsInEnum().WithMessage("Invalid payment type")
            .Must(x => Enum.IsDefined(typeof(PaymentType), x))
            .WithMessage("Payment type must be valid (CreditCard=1, Cash=2, EWallet=3, BankTransfer=4)");

        When(x => !string.IsNullOrEmpty(x.Request.Configuration), () =>
        {
            RuleFor(x => x.Request.Configuration)
                .Must(BeValidJson).WithMessage("Configuration must be valid JSON");
        });
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return true;
        
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

