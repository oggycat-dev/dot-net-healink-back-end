using FluentValidation;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;

public class RegisterSubscriptionCommandValidator : AbstractValidator<RegisterSubscriptionCommand>
{
    public RegisterSubscriptionCommandValidator()
    {
        RuleFor(x => x.Request.SubscriptionPlanId).NotEmpty().WithMessage("Subscription plan ID is required");
        RuleFor(x => x.Request.PaymentMethodId).NotEmpty().WithMessage("Payment method ID is required");
    }
}
