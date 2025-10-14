using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.Models.Momo;
using SharedLibrary.Commons.Enums;

namespace PaymentService.Application.Commons.Helpers;

/// <summary>
/// Helper class to validate gateway-specific responses
/// Returns validation result with error info if invalid
/// </summary>
public static class PaymentGatewayResponseValidator
{
    /// <summary>
    /// Validate gateway response based on type
    /// Returns (IsValid, ErrorCode, ErrorMessage)
    /// </summary>
    public static (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidateResponse(
        PaymentGatewayType gatewayType,
        object? gatewayResponse,
        ILogger logger)
    {
        if (gatewayResponse == null)
        {
            return (false, "NULL_RESPONSE", "Gateway returned null response");
        }

        return gatewayType switch
        {
            PaymentGatewayType.Momo => ValidateMomoResponse(gatewayResponse, logger),
            
            // FUTURE: Add other gateways here
            // PaymentGatewayType.VnPay => ValidateVnPayResponse(gatewayResponse, logger),
            // PaymentGatewayType.PayPal => ValidatePayPalResponse(gatewayResponse, logger),
            
            _ => throw new NotSupportedException($"Payment gateway '{gatewayType}' is not supported")
        };
    }

    /// <summary>
    /// Validate MoMo response
    /// MoMo ResultCode: 0 = Success, other = Error
    /// </summary>
    private static (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidateMomoResponse(
        object gatewayResponse,
        ILogger logger)
    {
        try
        {
            if (gatewayResponse is not MomoResponse momoResponse)
            {
                return (false, "INVALID_RESPONSE_TYPE", 
                    $"Expected MomoResponse but got {gatewayResponse.GetType().Name}");
            }

            // MoMo ResultCode: 0 = Success
            if (momoResponse.ResultCode != 0)
            {
                logger.LogWarning(
                    "MoMo payment initialization failed: ResultCode={ResultCode}, Message={Message}",
                    momoResponse.ResultCode, momoResponse.Message);

                return (false, 
                    momoResponse.ResultCode.ToString(), 
                    momoResponse.Message ?? "Payment initialization failed");
            }

            // ✅ Valid response
            return (true, null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating MoMo response");
            return (false, "VALIDATION_ERROR", $"Failed to validate response: {ex.Message}");
        }
    }

    /// <summary>
    /// FUTURE: Validate VnPay response
    /// VnPay ResponseCode: "00" = Success
    /// </summary>
    // private static (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidateVnPayResponse(
    //     object gatewayResponse,
    //     ILogger logger)
    // {
    //     if (gatewayResponse is not VnPayResponse vnPayResponse)
    //     {
    //         return (false, "INVALID_RESPONSE_TYPE", "Expected VnPayResponse");
    //     }
    //
    //     if (vnPayResponse.ResponseCode != "00")
    //     {
    //         return (false, vnPayResponse.ResponseCode, vnPayResponse.Message);
    //     }
    //
    //     return (true, null, null);
    // }

    /// <summary>
    /// FUTURE: Validate PayPal response
    /// </summary>
    // private static (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidatePayPalResponse(
    //     object gatewayResponse,
    //     ILogger logger)
    // {
    //     // PayPal validation logic
    // }
}

