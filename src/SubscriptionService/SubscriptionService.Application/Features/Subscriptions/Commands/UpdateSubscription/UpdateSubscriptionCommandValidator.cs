using FluentValidation;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.UpdateSubscription;

public class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
{
    public UpdateSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Subscription ID is required");

        RuleFor(x => x.Request.SubscriptionStatus)
            .IsInEnum().WithMessage("Invalid subscription status")
            .When(x => x.Request.SubscriptionStatus.HasValue);

        RuleFor(x => x.Request.RenewalBehavior)
            .IsInEnum().WithMessage("Invalid renewal behavior")
            .When(x => x.Request.RenewalBehavior.HasValue);

        RuleFor(x => x.Request.CurrentPeriodEnd)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Current period end must be in the future")
            .When(x => x.Request.CurrentPeriodEnd.HasValue);
    }
}
