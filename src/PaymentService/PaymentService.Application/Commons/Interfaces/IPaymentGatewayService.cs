using PaymentService.Application.Commons.Models;

namespace PaymentService.Application.Commons.Interfaces;

/// <summary>
/// Interface for payment gateway integration
/// Abstracts payment provider implementation (Stripe, PayPal, VNPay, Momo, etc.)
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Create a payment intent with payment provider
    /// </summary>
    Task<object?> CreatePaymentIntentAsync(
        object request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify IPN request from provider
    /// </summary>
    public bool VerifyIpnRequest(object ipnRequest);

    /// <summary>
    /// Refund a payment
    /// </summary>
    Task<object?> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment provider name
    /// </summary>
    string GetProviderName();

    /// <summary>
    /// Build signature for request
    /// </summary>
    string BuildSignature(Dictionary<string, string> parameters);

    /// <summary>
    /// Query transaction status from provider
    /// </summary>
    Task<object?> QueryTransactionAsync(object request, CancellationToken cancellationToken = default);
}

