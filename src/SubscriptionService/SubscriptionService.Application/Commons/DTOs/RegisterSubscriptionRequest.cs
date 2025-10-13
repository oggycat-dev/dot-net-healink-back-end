namespace SubscriptionService.Application.Commons.DTOs;

public class RegisterSubscriptionRequest
{
    public Guid SubscriptionPlanId { get; set; }
    public Guid PaymentMethodId { get; set; }
}