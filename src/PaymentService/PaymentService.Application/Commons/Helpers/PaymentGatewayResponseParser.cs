using PaymentService.Application.Commons.Models;
using PaymentService.Application.Commons.Models.Momo;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.Payment.Responses;

namespace PaymentService.Application.Commons.Helpers;

/// <summary>
/// Helper class to parse gateway-specific responses to unified PaymentIntentCreated
/// Supports multiple payment gateways (MoMo, VnPay, PayPal, etc.)
/// </summary>
public static class PaymentGatewayResponseParser
{
    /// <summary>
    /// Parse payment intent result wrapper to PaymentIntentCreated response
    /// </summary>
    public static PaymentIntentCreated ParseToPaymentIntentCreated(
        PaymentGatewayType gatewayType,
        object? resultWrapper,
        Guid subscriptionId)
    {
        if (resultWrapper == null)
        {
            return CreateErrorResponse(
                Guid.Empty,
                subscriptionId,
                "NULL_RESPONSE",
                "Gateway returned null response");
        }

        // Extract PaymentIntentResult wrapper
        if (resultWrapper is not PaymentIntentResult intentResult)
        {
            return CreateErrorResponse(
                Guid.Empty,
                subscriptionId,
                "INVALID_WRAPPER",
                $"Expected PaymentIntentResult but got {resultWrapper.GetType().Name}");
        }

        return gatewayType switch
        {
            PaymentGatewayType.Momo => ParseMomoResponse(
                intentResult.PaymentTransactionId, 
                intentResult.GatewayResponse, 
                subscriptionId),
            
            // FUTURE: Add other gateways here
            // PaymentGatewayType.VnPay => ParseVnPayResponse(intentResult, subscriptionId),
            // PaymentGatewayType.PayPal => ParsePayPalResponse(intentResult, subscriptionId),
            
            _ => CreateErrorResponse(
                intentResult.PaymentTransactionId,
                subscriptionId,
                "UNSUPPORTED_GATEWAY",
                $"Payment gateway '{gatewayType}' is not supported")
        };
    }

    /// <summary>
    /// Parse MoMo gateway response
    /// </summary>
    private static PaymentIntentCreated ParseMomoResponse(
        Guid paymentTransactionId,
        object gatewayResponse, 
        Guid subscriptionId)
    {
        if (gatewayResponse is not MomoResponse momoResponse)
        {
            return CreateErrorResponse(
                paymentTransactionId,
                subscriptionId,
                "INVALID_RESPONSE_TYPE",
                $"Expected MomoResponse but got {gatewayResponse.GetType().Name}");
        }

        var isSuccess = momoResponse.ResultCode == 0;

        return new PaymentIntentCreated
        {
            Success = isSuccess,
            Message = isSuccess 
                ? "Payment intent created successfully" 
                : $"MoMo error: {momoResponse.Message}",
            SubscriptionId = subscriptionId,
            PaymentTransactionId = paymentTransactionId,  // ✅ From database, not from MoMo
            PaymentUrl = momoResponse.PayUrl,
            QrCodeUrl = momoResponse.QrCodeUrl,
            DeepLink = momoResponse.DeepLink,
            AppLink = momoResponse.DeepLinkWebInApp,  // ✅ For in-app browser
            ErrorCode = isSuccess ? null : momoResponse.ResultCode.ToString(),
            ErrorMessage = isSuccess ? null : momoResponse.Message,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = momoResponse.OrderId,          // ✅ MoMo's OrderId (SubscriptionId)
                ["requestId"] = momoResponse.RequestId,      // ✅ MoMo's RequestId (REQ_{subscriptionId})
                ["gatewayType"] = PaymentGatewayType.Momo.ToString(),
                ["resultCode"] = momoResponse.ResultCode.ToString(),
                ["deepLinkWebInApp"] = momoResponse.DeepLinkWebInApp ?? ""  // ✅ For debugging
            }
        };
    }

    /// <summary>
    /// FUTURE: Parse VnPay gateway response
    /// </summary>
    // private static PaymentIntentCreated ParseVnPayResponse(object gatewayResponse, Guid subscriptionId)
    // {
    //     if (gatewayResponse is not VnPayResponse vnPayResponse)
    //     {
    //         return CreateErrorResponse(subscriptionId, "INVALID_RESPONSE_TYPE", 
    //             $"Expected VnPayResponse but got {gatewayResponse.GetType().Name}");
    //     }
    //
    //     var isSuccess = vnPayResponse.ResponseCode == "00";
    //     return new PaymentIntentCreated
    //     {
    //         Success = isSuccess,
    //         PaymentUrl = vnPayResponse.PaymentUrl,
    //         // ... map other fields
    //     };
    // }

    /// <summary>
    /// Create error response
    /// </summary>
    private static PaymentIntentCreated CreateErrorResponse(
        Guid paymentTransactionId,
        Guid subscriptionId,
        string errorCode,
        string errorMessage)
    {
        return new PaymentIntentCreated
        {
            Success = false,
            Message = "Failed to create payment intent",
            PaymentTransactionId = paymentTransactionId,
            SubscriptionId = subscriptionId,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

