using MediatR;
using PaymentService.Application.Commons.Models.Momo;
using SharedLibrary.Commons.Models;

namespace PaymentService.Application.Features.Payments.Commands.VerifyMomoIpn;

/// <summary>
/// Command to verify and process MoMo IPN (Instant Payment Notification) callback
/// Strictly follows MoMo AIO v2 specification
/// Reference: https://developers.momo.vn/v2/#/docs/aiov2/
/// </summary>
public record VerifyMomoIpnCommand : IRequest<Result<MoMoIpnResponse>>
{
    /// <summary>
    /// IPN request from MoMo gateway
    /// Must contain all required fields per MoMo specification:
    /// - partnerCode, orderId, requestId, amount, orderInfo, orderType
    /// - transId, resultCode, message, payType, responseTime
    /// - extraData, signature
    /// </summary>
    public MoMoIpnRequest IpnRequest { get; init; } = null!;
    
    /// <summary>
    /// IP address of the caller (for MoMo IP whitelist validation)
    /// Must be from official MoMo IPs: 171.244.48.0/20, 203.162.71.0/24, etc.
    /// </summary>
    public string CallerIpAddress { get; init; } = string.Empty;
}

