using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commons.Models.Momo;
using PaymentService.Application.Features.Payments.Commands.VerifyMomoIpn;

namespace PaymentService.API.Controllers;

/// <summary>
/// Payment callback controller for receiving IPN (Instant Payment Notification) from payment gateways
/// Each gateway has its own dedicated endpoint with specific validation and response format
/// </summary>
[ApiController]
[Route("api/payment-callback")]
public class PaymentCallbackController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentCallbackController> _logger;

    public PaymentCallbackController(
        IMediator mediator,
        ILogger<PaymentCallbackController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// MoMo IPN callback endpoint (AIO v2 specification)
    /// Receives payment notification from MoMo gateway
    /// Reference: https://developers.momo.vn/v2/#/docs/aiov2/
    /// 
    /// Security:
    /// - IP whitelist validation (only MoMo official IPs)
    /// - HMAC SHA256 signature verification
    /// - Idempotency handling (duplicate IPN safe)
    /// 
    /// Response Format:
    /// - Always return HTTP 200 OK with JSON body (per MoMo spec)
    /// - Never return 4xx/5xx status codes
    /// - Response must include signature
    /// </summary>
    /// <param name="ipnRequest">IPN request from MoMo gateway</param>
    /// <returns>IPN response for MoMo (always 200 OK)</returns>
    [HttpPost("momo/ipn")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> MomoIpnCallback([FromBody] MoMoIpnRequest ipnRequest)
    {
        // Get caller IP address (with proxy support)
        var callerIp = GetCallerIpAddress();

        _logger.LogInformation(
            "[MoMo IPN] Received callback from IP: {IP}, OrderId: {OrderId}, TransId: {TransId}, ResultCode: {ResultCode}",
            callerIp, ipnRequest.OrderId, ipnRequest.TransId, ipnRequest.ResultCode);

        // Send command to MoMo-specific handler
        var command = new VerifyMomoIpnCommand
        {
            IpnRequest = ipnRequest,
            CallerIpAddress = callerIp
        };

        var result = await _mediator.Send(command);

        // ✅ Per MoMo spec: Always return 200 OK with JSON body
        // Never return 4xx/5xx - use resultCode in body instead
        if (result.IsSuccess && result.Data != null)
        {
            _logger.LogInformation(
                "[MoMo IPN] Response sent: OrderId={OrderId}, ResultCode={ResultCode}",
                ipnRequest.OrderId, result.Data.ResultCode);
            
            return Ok(result.Data);
        }

        // Error case - still return 200 OK with error body (per MoMo spec)
        _logger.LogError(
            "[MoMo IPN] Error processing: OrderId={OrderId}, Message={Message}",
            ipnRequest.OrderId, result.Message);
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Get caller IP address from request
    /// Handles X-Forwarded-For and X-Real-IP headers for proxies/load balancers
    /// Priority: X-Forwarded-For (first IP) > X-Real-IP > RemoteIpAddress
    /// </summary>
    private string GetCallerIpAddress()
    {
        // Check X-Forwarded-For header (common in load balancers like AWS ALB, Nginx)
        // Format: "client, proxy1, proxy2, ..."
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim(); // First IP is the original client
            }
        }

        // Check X-Real-IP header (common in Nginx)
        if (Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString().Trim();
        }

        // Fallback to RemoteIpAddress (direct connection)
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Health check endpoint for payment callback service
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "payment-callback",
            timestamp = DateTime.UtcNow,
            endpoints = new[]
            {
                "/api/payment-callback/momo/ipn - MoMo IPN callback (AIO v2)"
            }
        });
    }
}
