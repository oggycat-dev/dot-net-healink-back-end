using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Events;

/// <summary>
/// Event published when user initiates subscription registration
/// Triggers the subscription saga workflow
/// Inherits from IntegrationEvent to capture IP/UserAgent for activity logging
/// </summary>
public record SubscriptionRegistrationStarted : IntegrationEvent
{
    /// <summary>
    /// Subscription ID - used as CorrelationId for saga tracking
    /// </summary>
    public Guid SubscriptionId { get; init; }
    
    public Guid UserProfileId { get; init; }
    public Guid SubscriptionPlanId { get; init; }
    public Guid PaymentMethodId { get; init; }
    public string SubscriptionPlanName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    
    public SubscriptionRegistrationStarted() : base("SubscriptionService") { }
}

