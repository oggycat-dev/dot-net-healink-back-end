using SharedLibrary.Commons.EventBus;

namespace SharedLibrary.Contracts.Subscription.Commands;

/// <summary>
/// Command sent to PaymentService to request payment processing
/// </summary>
public record RequestPayment : IntegrationEvent
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid PaymentMethodId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; init; }
    public string? UserAgent { get; init; } // ✅ For client type detection
}

