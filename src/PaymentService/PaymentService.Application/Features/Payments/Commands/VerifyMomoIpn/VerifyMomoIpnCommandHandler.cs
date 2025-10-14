using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.Constants;
using PaymentService.Application.Commons.Interfaces;
using PaymentService.Application.Commons.Models.Momo;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Payment.Events;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Application.Features.Payments.Commands.VerifyMomoIpn;

/// <summary>
/// Handler for verifying MoMo IPN callbacks (AIO v2 specification)
/// Strictly follows MoMo documentation: https://developers.momo.vn/v2/#/docs/aiov2/
/// 
/// Process Flow:
/// 1. Validate IP whitelist (only MoMo official IPs)
/// 2. Verify HMAC SHA256 signature
/// 3. Find payment transaction by OrderId (SubscriptionId)
/// 4. Check idempotency (duplicate IPN handling)
/// 5. Process payment result (success/failure)
/// 6. Update transaction status
/// 7. Publish events to saga (PaymentSucceeded/PaymentFailed)
/// 8. Return proper IPN response with signature
/// </summary>
public class VerifyMomoIpnCommandHandler : IRequestHandler<VerifyMomoIpnCommand, Result<MoMoIpnResponse>>
{
    private readonly IOutboxUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifyMomoIpnCommandHandler> _logger;
    private readonly HashSet<string> _ipnWhitelist;

    public VerifyMomoIpnCommandHandler(
        IOutboxUnitOfWork unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<VerifyMomoIpnCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
        _logger = logger;
        
        // ✅ Load IP whitelist from configuration (environment variables)
        // Format: Comma-separated IPs or CIDR ranges in MOMO_IPN_WHITELIST
        // MoMo official IPs per documentation (multiple IPs in same subnet)
        // Default includes CIDR range to cover all MoMo IPs: 118.69.208.0/20 covers 118.69.208.0 to 118.69.223.255
        var whitelistConfig = _configuration.GetValue<string>("Momo:IpnWhitelist") 
            ?? "118.69.208.0/20,210.245.113.71,127.0.0.1,::1";
        
        _ipnWhitelist = whitelistConfig
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ip => ip.Trim())
            .ToHashSet();
        
        _logger.LogInformation(
            "[MoMo IPN] Loaded {Count} IPs to whitelist: {IPs}",
            _ipnWhitelist.Count, string.Join(", ", _ipnWhitelist));
    }

    public async Task<Result<MoMoIpnResponse>> Handle(
        VerifyMomoIpnCommand request, 
        CancellationToken cancellationToken)
    {
        var ipnRequest = request.IpnRequest;
        
        _logger.LogInformation(
            "[MoMo IPN] Processing: OrderId={OrderId}, TransId={TransId}, ResultCode={ResultCode}, IP={IP}",
            ipnRequest.OrderId, ipnRequest.TransId, ipnRequest.ResultCode, request.CallerIpAddress);

        try
        {
            // ✅ Step 1: Validate IP whitelist (MoMo official IPs only)
            if (!ValidateMomoIpWhitelist(request.CallerIpAddress))
            {
                _logger.LogWarning(
                    "[MoMo IPN] REJECTED - Unauthorized IP: {IP}, OrderId={OrderId}",
                    request.CallerIpAddress, ipnRequest.OrderId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.InvalidSignature, 
                    "Unauthorized IP address");
            }

            // ✅ Step 2: Validate required fields per MoMo spec
            var validationError = ValidateMomoIpnRequest(ipnRequest);
            if (validationError != null)
            {
                _logger.LogError(
                    "[MoMo IPN] REJECTED - Invalid request: {Error}, OrderId={OrderId}",
                    validationError, ipnRequest.OrderId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.InvalidSignature, 
                    validationError);
            }

            // ✅ Step 3: Get MoMo service and verify signature (HMAC SHA256)
            var momoService = _paymentGatewayFactory.GetPaymentGatewayService(PaymentGatewayType.Momo);
            
            if (!momoService.VerifyIpnRequest(ipnRequest))
            {
                _logger.LogError(
                    "[MoMo IPN] REJECTED - Invalid signature: OrderId={OrderId}, TransId={TransId}",
                    ipnRequest.OrderId, ipnRequest.TransId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.InvalidSignature, 
                    "Invalid signature");
            }

            // ✅ Step 4: Find payment transaction by OrderId (= SubscriptionId)
            if (!Guid.TryParse(ipnRequest.OrderId, out var subscriptionId))
            {
                _logger.LogError(
                    "[MoMo IPN] REJECTED - Invalid OrderId format: {OrderId}", 
                    ipnRequest.OrderId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.TransactionNotFound, 
                    "Invalid order ID format");
            }

            var transaction = await _unitOfWork.Repository<PaymentTransaction>()
                .GetFirstOrDefaultAsync(
                    t => t.ReferenceId == subscriptionId && 
                         t.TransactionType == TransactionType.Subscription);

            if (transaction == null)
            {
                _logger.LogError(
                    "[MoMo IPN] REJECTED - Transaction not found: OrderId={OrderId}", 
                    ipnRequest.OrderId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.TransactionNotFound, 
                    "Transaction not found");
            }

            // ✅ Step 5: Check for duplicate IPN (idempotency per MoMo spec)
            // MoMo may send duplicate IPNs - merchant must handle gracefully
            if (transaction.PaymentStatus == PayementStatus.Succeeded && 
                !string.IsNullOrEmpty(transaction.TransactionId))
            {
                _logger.LogInformation(
                    "[MoMo IPN] DUPLICATE - Already processed: OrderId={OrderId}, TransId={TransId}",
                    ipnRequest.OrderId, ipnRequest.TransId);
                
                // Return success for duplicate (idempotent - as per MoMo spec)
                return CreateMomoSuccessResponse(ipnRequest);
            }

            // ✅ Step 6: Validate amount matches (security check)
            if (transaction.Amount != (decimal)ipnRequest.Amount)
            {
                _logger.LogError(
                    "[MoMo IPN] REJECTED - Amount mismatch: Expected={Expected}, Received={Received}, OrderId={OrderId}",
                    transaction.Amount, ipnRequest.Amount, ipnRequest.OrderId);
                
                return CreateMomoErrorResponse(
                    ipnRequest, 
                    MomoConstants.ResultCodes.InvalidAmount, 
                    "Amount mismatch");
            }

            // ✅ Step 7: Process payment result per MoMo result codes
            // ResultCode = 0: Success, Other: Failed
            var isPaymentSuccess = ipnRequest.ResultCode == MomoConstants.ResultCodes.Success;

            if (isPaymentSuccess)
            {
                // ✅ SUCCESS CASE: Update transaction to Succeeded
                transaction.PaymentStatus = PayementStatus.Succeeded;
                transaction.TransactionId = ipnRequest.TransId.ToString(); // MoMo's TransId
                transaction.UpdateEntity(null); // System update
                
                _logger.LogInformation(
                    "[MoMo IPN] SUCCESS: OrderId={OrderId}, TransId={TransId}, Amount={Amount}",
                    ipnRequest.OrderId, ipnRequest.TransId, ipnRequest.Amount);

                // ✅ Publish PaymentSucceeded event to saga
                await _publishEndpoint.Publish(new PaymentSucceeded
                {
                    PaymentIntentId = transaction.Id,
                    SubscriptionId = subscriptionId,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency,
                    PaymentProvider = PaymentGatewayType.Momo.ToString(),
                    TransactionId = ipnRequest.TransId.ToString(),
                    PaidAt = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                // ✅ FAILURE CASE: Update transaction to Failed
                transaction.PaymentStatus = PayementStatus.Failed;
                transaction.ErrorCode = ipnRequest.ResultCode.ToString();
                transaction.ErrorMessage = ipnRequest.Message;
                transaction.UpdateEntity(null); // System update
                
                _logger.LogError(
                    "[MoMo IPN] FAILED: OrderId={OrderId}, ResultCode={ResultCode}, Message={Message}",
                    ipnRequest.OrderId, ipnRequest.ResultCode, ipnRequest.Message);

                // ✅ Publish PaymentFailed event to saga
                await _publishEndpoint.Publish(new PaymentFailed
                {
                    PaymentIntentId = transaction.Id,
                    SubscriptionId = subscriptionId,
                    Reason = $"MoMo payment failed (Code {ipnRequest.ResultCode}): {ipnRequest.Message}",
                    ErrorCode = ipnRequest.ResultCode.ToString(),
                    ErrorMessage = ipnRequest.Message,
                    FailedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            // ✅ Step 8: Save transaction updates
            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[MoMo IPN] PROCESSED: OrderId={OrderId}, Status={Status}",
                ipnRequest.OrderId, transaction.PaymentStatus);

            // ✅ Step 9: Return success response to MoMo (per spec: always 200 OK with body)
            return CreateMomoSuccessResponse(ipnRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[MoMo IPN] EXCEPTION: OrderId={OrderId}", 
                ipnRequest.OrderId);
            
            return CreateMomoErrorResponse(
                ipnRequest, 
                MomoConstants.ResultCodes.SystemError, 
                "Internal server error");
        }
    }

    /// <summary>
    /// Validate MoMo IPN request fields per specification
    /// All fields are required as per MoMo docs
    /// </summary>
    private string? ValidateMomoIpnRequest(MoMoIpnRequest ipnRequest)
    {
        if (string.IsNullOrWhiteSpace(ipnRequest.PartnerCode))
            return "Missing partnerCode";
        
        if (string.IsNullOrWhiteSpace(ipnRequest.OrderId))
            return "Missing orderId";
        
        if (string.IsNullOrWhiteSpace(ipnRequest.RequestId))
            return "Missing requestId";
        
        if (ipnRequest.Amount <= 0)
            return "Invalid amount";
        
        if (ipnRequest.TransId <= 0)
            return "Invalid transId";
        
        if (string.IsNullOrWhiteSpace(ipnRequest.Signature))
            return "Missing signature";

        return null; // Valid
    }

    /// <summary>
    /// Validate caller IP against MoMo official IP whitelist (loaded from configuration)
    /// Per MoMo spec: Only requests from official IPs are valid
    /// Supports both exact IP and CIDR notation
    /// </summary>
    private bool ValidateMomoIpWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        // Check exact match first
        if (_ipnWhitelist.Contains(ipAddress))
            return true;

        // Check CIDR ranges
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return false;

        foreach (var whitelistedEntry in _ipnWhitelist)
        {
            if (whitelistedEntry.Contains('/'))
            {
                // CIDR notation
                if (IsIpInCidrRange(ip, whitelistedEntry))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if IP is within CIDR range
    /// Example: 171.244.48.0/20 covers 171.244.48.0 to 171.244.63.255
    /// </summary>
    private bool IsIpInCidrRange(IPAddress ip, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            var baseAddress = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var ipBytes = ip.GetAddressBytes();
            var baseBytes = baseAddress.GetAddressBytes();

            if (ipBytes.Length != baseBytes.Length)
                return false;

            var fullBytes = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            // Check full bytes
            for (int i = 0; i < fullBytes; i++)
            {
                if (ipBytes[i] != baseBytes[i])
                    return false;
            }

            // Check remaining bits
            if (remainingBits > 0)
            {
                var mask = (byte)(0xFF << (8 - remainingBits));
                if ((ipBytes[fullBytes] & mask) != (baseBytes[fullBytes] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create success IPN response per MoMo specification
    /// Must include: partnerCode, requestId, orderId, resultCode, message, responseTime, extraData, signature
    /// </summary>
    private Result<MoMoIpnResponse> CreateMomoSuccessResponse(MoMoIpnRequest ipnRequest)
    {
        var responseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var response = new MoMoIpnResponse
        {
            PartnerCode = ipnRequest.PartnerCode,
            RequestId = ipnRequest.RequestId,
            OrderId = ipnRequest.OrderId,
            ResultCode = MomoConstants.ResultCodes.Success,
            Message = "Success",
            ResponseTime = responseTime,
            ExtraData = ipnRequest.ExtraData ?? string.Empty,
            Signature = BuildMomoResponseSignature(
                ipnRequest, 
                MomoConstants.ResultCodes.Success, 
                "Success", 
                responseTime)
        };

        return Result<MoMoIpnResponse>.Success(response, "IPN processed successfully");
    }

    /// <summary>
    /// Create error IPN response per MoMo specification
    /// Merchant must return proper response even for errors (not HTTP 4xx/5xx)
    /// </summary>
    private Result<MoMoIpnResponse> CreateMomoErrorResponse(
        MoMoIpnRequest ipnRequest, 
        int resultCode, 
        string message)
    {
        var responseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var response = new MoMoIpnResponse
        {
            PartnerCode = ipnRequest.PartnerCode,
            RequestId = ipnRequest.RequestId,
            OrderId = ipnRequest.OrderId,
            ResultCode = resultCode,
            Message = message,
            ResponseTime = responseTime,
            ExtraData = ipnRequest.ExtraData ?? string.Empty,
            Signature = BuildMomoResponseSignature(ipnRequest, resultCode, message, responseTime)
        };

        return Result<MoMoIpnResponse>.Success(response, message);
    }

    /// <summary>
    /// Build HMAC SHA256 signature for MoMo IPN response
    /// Per MoMo spec: accessKey, extraData, message, orderId, partnerCode, requestId, responseTime, resultCode (alphabetically sorted)
    /// Algorithm: HMAC_SHA256(rawSignature, secretKey)
    /// </summary>
    private string BuildMomoResponseSignature(
        MoMoIpnRequest ipnRequest, 
        int resultCode, 
        string message,
        long responseTime)
    {
        var accessKey = _configuration.GetValue<string>("Momo:AccessKey") 
            ?? throw new InvalidOperationException("Momo:AccessKey not configured");
        var secretKey = _configuration.GetValue<string>("Momo:SecretKey") 
            ?? throw new InvalidOperationException("Momo:SecretKey not configured");

        // ✅ Per MoMo spec: Parameters must be alphabetically sorted
        var parameters = new Dictionary<string, string>
        {
            { "accessKey", accessKey },
            { "extraData", ipnRequest.ExtraData ?? string.Empty },
            { "message", message },
            { "orderId", ipnRequest.OrderId },
            { "partnerCode", ipnRequest.PartnerCode },
            { "requestId", ipnRequest.RequestId },
            { "responseTime", responseTime.ToString() },
            { "resultCode", resultCode.ToString() }
        };

        // Sort parameters alphabetically
        var sortedParams = parameters.OrderBy(x => x.Key);

        // Build raw signature string: key1=value1&key2=value2&...
        var rawSignature = string.Join("&",
            sortedParams.Select(p => $"{p.Key}={p.Value}"));

        _logger.LogDebug("[MoMo IPN] Response signature raw: {RawSignature}", rawSignature);

        // Calculate HMAC SHA256
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));

        return BitConverter.ToString(hashBytes)
            .Replace("-", "")
            .ToLower();
    }
}

