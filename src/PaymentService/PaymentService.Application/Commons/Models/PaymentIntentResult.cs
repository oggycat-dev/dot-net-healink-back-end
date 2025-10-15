namespace PaymentService.Application.Commons.Models;

/// <summary>
/// Wrapper for payment intent creation result
/// Contains both internal transaction ID and gateway-specific response
/// </summary>
public record PaymentIntentResult
{
    /// <summary>
    /// Internal PaymentTransaction ID (database record)
    /// </summary>
    public Guid PaymentTransactionId { get; init; }
    
    /// <summary>
    /// Gateway-specific response object (MomoResponse, VnPayResponse, etc.)
    /// </summary>
    public object GatewayResponse { get; init; } = null!;
}

