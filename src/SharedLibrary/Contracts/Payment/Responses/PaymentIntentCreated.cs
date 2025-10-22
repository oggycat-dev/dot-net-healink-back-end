namespace SharedLibrary.Contracts.Payment.Responses;

/// <summary>
/// Response after creating payment intent
/// Contains gateway-specific data for frontend (PayUrl, QrCodeUrl, etc.)
/// </summary>
public record PaymentIntentCreated
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid PaymentTransactionId { get; init; }
    public Guid SubscriptionId { get; init; }
    
    // Gateway response data (varies by provider)
    public string? PaymentUrl { get; init; }
    public string? QrCodeUrl { get; init; }
    public string? DeepLink { get; init; }
    public string? AppLink { get; init; }  // ✅ For in-app browser (MoMo DeepLinkWebInApp)
    
    // Error info (if failed)
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    
    // Additional metadata
    public Dictionary<string, string> Metadata { get; init; } = new();
}

