using FluentValidation;

namespace PaymentService.Application.Features.PaymentMethods.Commands.UpdatePaymentMethod;

public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Request.ProviderName)
            .NotEmpty().WithMessage("Provider name is required")
            .MaximumLength(100).WithMessage("Provider name must not exceed 100 characters");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

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

