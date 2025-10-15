using FluentValidation;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    public CreateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Request.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than or equal to 0");

        RuleFor(x => x.Request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .MaximumLength(3).WithMessage("Currency must be 3 characters (e.g., VND, USD)");

        RuleFor(x => x.Request.BillingPeriodCount)
            .GreaterThan(0).WithMessage("Billing period count must be greater than 0");

        RuleFor(x => x.Request.BillingPeriodUnit)
            .IsInEnum().WithMessage("Invalid billing period unit")
            .Must(x => Enum.IsDefined(typeof(BillingPeriodUnit), x))
            .WithMessage("Billing period unit must be 1 (Month) or 2 (Year)");

        RuleFor(x => x.Request.TrialDays)
            .GreaterThanOrEqualTo(0).WithMessage("Trial days must be greater than or equal to 0")
            .LessThanOrEqualTo(90).WithMessage("Trial days must not exceed 90 days");

        When(x => !string.IsNullOrEmpty(x.Request.FeatureConfig), () =>
        {
            RuleFor(x => x.Request.FeatureConfig)
                .Must(BeValidJson).WithMessage("Feature config must be valid JSON");
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
