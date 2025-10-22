using PaymentService.Domain.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.Subscription.Commands;

namespace PaymentService.Application.Commons.Helpers;

/// <summary>
/// Helper class to build gateway-specific payment requests
/// Supports multiple payment gateways (MoMo, VnPay, PayPal, etc.)
/// </summary>
public static class PaymentGatewayRequestBuilder
{
    /// <summary>
    /// Build gateway-specific request based on type
    /// </summary>
    public static object BuildPaymentRequest(
        PaymentGatewayType gatewayType,
        Guid subscriptionId,
        Guid userProfileId,
        Guid paymentMethodId,
        decimal amount,
        string currency,
        string description,
        Dictionary<string, string>? metadata,
        PaymentTransaction transaction,
        string? userAgent = null) // ✅ Add UserAgent parameter
    {
        return gatewayType switch
        {
            PaymentGatewayType.Momo => BuildMomoRequest(
                subscriptionId, userProfileId, paymentMethodId, 
                amount, currency, description, metadata, transaction, userAgent),
            
            // FUTURE: Add other gateways here
            // PaymentGatewayType.VnPay => BuildVnPayRequest(...),
            // PaymentGatewayType.PayPal => BuildPayPalRequest(...),
            
            _ => throw new NotSupportedException($"Payment gateway '{gatewayType}' is not supported")
        };
    }

    /// <summary>
    /// Build MoMo-specific request
    /// </summary>
    private static RequestPayment BuildMomoRequest(
        Guid subscriptionId,
        Guid userProfileId,
        Guid paymentMethodId,
        decimal amount,
        string currency,
        string description,
        Dictionary<string, string>? metadata,
        PaymentTransaction transaction,
        string? userAgent = null) // ✅ Add UserAgent parameter
    {
        var requestMetadata = metadata ?? new Dictionary<string, string>();
        
        // Add transaction tracking metadata
        requestMetadata["internalTransactionId"] = transaction.Id.ToString();
        requestMetadata["transactionType"] = transaction.TransactionType.ToString();
        requestMetadata["gatewayType"] = PaymentGatewayType.Momo.ToString();
        
        return new RequestPayment
        {
            // ✅ CRITICAL: SubscriptionId becomes MoMo's OrderId
            SubscriptionId = subscriptionId,
            UserProfileId = userProfileId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Currency = currency,
            Description = description,
            Metadata = requestMetadata,
            UserAgent = userAgent // ✅ Pass UserAgent to RequestPayment
        };
    }

    /// <summary>
    /// FUTURE: Build VnPay-specific request
    /// </summary>
    // private static VnPayRequest BuildVnPayRequest(...)
    // {
    //     return new VnPayRequest
    //     {
    //         OrderId = subscriptionId.ToString(),
    //         Amount = amount,
    //         // ... VnPay-specific fields
    //     };
    // }

    /// <summary>
    /// FUTURE: Build PayPal-specific request
    /// </summary>
    // private static PayPalRequest BuildPayPalRequest(...)
    // {
    //     return new PayPalRequest
    //     {
    //         OrderId = subscriptionId.ToString(),
    //         Amount = amount,
    //         // ... PayPal-specific fields
    //     };
    // }
}

