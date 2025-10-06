using FluentValidation;

namespace SubscriptionService.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;

public class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID is required");

        // DisplayName is required
        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

        // Description is required
        RuleFor(x => x.Request.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        // Amount validation (required, not nullable)
        RuleFor(x => x.Request.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than or equal to 0");

        // TrialDays validation (required, not nullable)
        RuleFor(x => x.Request.TrialDays)
            .GreaterThanOrEqualTo(0).WithMessage("Trial days must be greater than or equal to 0")
            .LessThanOrEqualTo(90).WithMessage("Trial days must not exceed 90 days");

        // BillingPeriodCount validation (required, not nullable)
        RuleFor(x => x.Request.BillingPeriodCount)
            .GreaterThan(0).WithMessage("Billing period count must be greater than 0");

        // FeatureConfig validation (if provided, must be valid JSON)
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
