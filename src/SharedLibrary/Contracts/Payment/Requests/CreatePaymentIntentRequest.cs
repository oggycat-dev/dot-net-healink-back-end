namespace SharedLibrary.Contracts.Payment.Requests;

/// <summary>
/// Request-Response contract for creating payment intent
/// Frontend needs immediate response with PayUrl/QrCodeUrl
/// </summary>
public record CreatePaymentIntentRequest
{
    public Guid SubscriptionId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid PaymentMethodId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; init; }
    public Guid? CreatedBy { get; init; }
    public string? UserAgent { get; init; } // ✅ For client type detection
}

