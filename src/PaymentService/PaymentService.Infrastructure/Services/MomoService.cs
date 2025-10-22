using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commons.Interfaces;
using PaymentService.Application.Commons.Models;
using PaymentService.Application.Commons.Models.Momo;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.Subscription.Commands;

namespace PaymentService.Infrastructure.Services;

public class MomoService : IPaymentGatewayService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MomoService> _logger;
    
    // ✅ Cached config values - loaded once
    private readonly string _partnerCode;
    private readonly string _partnerName;
    private readonly string _storeId;
    private readonly string _ipnUrl;
    private readonly string _redirectUrl;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _apiEndpoint;
    
    public MomoService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<MomoService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        // ✅ Load all configs once in constructor
        _partnerCode = GetRequiredConfig("Momo:PartnerCode");
        _partnerName = GetRequiredConfig("Momo:PartnerName");
        _storeId = GetRequiredConfig("Momo:StoreId");
        _ipnUrl = GetRequiredConfig("Momo:IpnUrl");
        _redirectUrl = GetRequiredConfig("Momo:RedirectUrl");
        _accessKey = GetRequiredConfig("Momo:AccessKey");
        _secretKey = GetRequiredConfig("Momo:SecretKey");
        _apiEndpoint = GetRequiredConfig("Momo:ApiEndpoint");
    }
    
    /// <summary>
    /// Helper method to get required config value
    /// </summary>
    private string GetRequiredConfig(string key)
    {
        return _config.GetValue<string>(key) 
            ?? throw new InvalidOperationException($"Configuration '{key}' is missing. Please set it in environment variables or appsettings.json");
    }

    public async Task<object?> CreatePaymentIntentAsync(object request, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestPayment = request as RequestPayment ?? throw new ArgumentException("Invalid request type", nameof(request));
            
            var requestType = "captureWallet"; // ✅ MoMo AIO v2: captureWallet | qrcode | payWithATM
            var lang = "vi";
            var requestId = $"REQ_{requestPayment.SubscriptionId}";

            // ✅ MoMo ExtraData: Base64 encode JSON format
            var extraData = string.Empty;
            if (requestPayment.Metadata != null && requestPayment.Metadata.Any())
            {
                var jsonData = JsonSerializer.Serialize(requestPayment.Metadata);
                extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
            }

            var amount = (long)requestPayment.Amount;
            var orderId = requestPayment.SubscriptionId.ToString();
            var orderInfo = requestPayment.Description;
            
            // ✅ Detect client type from User-Agent header or metadata
            var userAgent = requestPayment.UserAgent ?? "";
            var isFlutterApp = userAgent.Contains("Flutter") || userAgent.Contains("Dart");
            var isMobileApp = userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iOS");
            
            // ✅ Set appropriate redirect URL based on client type
            string redirectUrl;
            if (isFlutterApp || isMobileApp)
            {
                // ✅ Flutter app redirect - use custom scheme
                redirectUrl = "healink://payment/result";
            }
            else
            {
                // ✅ Web app redirect - use web URL
                redirectUrl = _redirectUrl; // Use configured web redirect URL
            }
            
            _logger.LogInformation(
                "MoMo redirect configured: UserAgent={UserAgent}, IsFlutter={IsFlutter}, RedirectUrl={RedirectUrl}",
                userAgent, isFlutterApp, redirectUrl);

            // Build signature
            var parameters = new Dictionary<string, string>
            {
                { "accessKey", _accessKey },
                { "partnerCode", _partnerCode },
                { "requestType", requestType },
                { "ipnUrl", _ipnUrl },
                { "redirectUrl", redirectUrl }, // ✅ Use dynamic redirect URL
                { "orderId", orderId },
                { "amount", amount.ToString() },
                { "orderInfo", orderInfo },
                { "requestId", requestId },
                { "extraData", extraData }
            };
            
            var signature = BuildSignature(parameters);
            
            var momoRequest = new CreateMomoRequest
            {
                PartnerCode = _partnerCode,
                PartnerName = _partnerName,
                StoreId = _storeId,
                RequestType = requestType,
                IpnUrl = _ipnUrl,
                RedirectUrl = redirectUrl, // ✅ Use dynamic redirect URL
                OrderId = orderId,
                Amount = amount,
                Lang = lang,
                OrderInfo = orderInfo,
                RequestId = requestId,
                ExtraData = extraData,
                Signature = signature
            };
            
            // ✅ Call MoMo API
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync($"{_apiEndpoint}/create", momoRequest, cancellationToken);

            // ✅ Validate HTTP response
            response.EnsureSuccessStatusCode();

            // ✅ Parse and validate MoMo response
            var result = await response.Content.ReadFromJsonAsync<MomoResponse>(options: new JsonSerializerOptions { PropertyNamingPolicy  = JsonNamingPolicy.CamelCase }, cancellationToken)
                ?? throw new Exception("Failed to parse MoMo response");

            // ✅ Log response details for debugging
            _logger.LogInformation("MoMo Response - PayUrl: {PayUrl}, DeepLink: {DeepLink}, DeepLinkWebInApp: {DeepLinkWebInApp}", 
                result.PayUrl, result.DeepLink, result.DeepLinkWebInApp);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            throw;
        }

    }

    /// <summary>
    /// Build HMAC SHA256 signature for MoMo request
    /// </summary>
    public string BuildSignature(Dictionary<string, string> parameters)
    {
        // Sort parameters alphabetically
        var sortedParams = parameters.OrderBy(x => x.Key);

        // Build raw signature string
        var rawSignature = string.Join("&",
            sortedParams.Select(p => $"{p.Key}={p.Value}"));

        // Calculate HMAC SHA256
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawSignature));

        return BitConverter.ToString(hashBytes)
            .Replace("-", "")
            .ToLower();
    }

    public string GetProviderName()
    {
        return PaymentGatewayType.Momo.ToString();
    }

    public Task<object?> RefundPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public bool VerifyIpnRequest(object ipnRequest)
    {
        try
        {
            // Cast ipnRequest to MoMoIpnRequest
            var moMoIpnRequest = ipnRequest as MoMoIpnRequest ?? throw new ArgumentException("Invalid IPN request type", nameof(ipnRequest));
            
            // Build signature
            var parameters = new Dictionary<string, string>
            {
                { "accessKey", _accessKey },
                { "amount", moMoIpnRequest.Amount.ToString() },
                { "extraData", moMoIpnRequest.ExtraData },
                { "message", moMoIpnRequest.Message },
                { "orderId", moMoIpnRequest.OrderId },
                { "orderInfo", moMoIpnRequest.OrderInfo },
                { "orderType", moMoIpnRequest.OrderType },
                { "partnerCode", moMoIpnRequest.PartnerCode },
                { "payType", moMoIpnRequest.PayType },
                { "requestId", moMoIpnRequest.RequestId },
                { "responseTime", moMoIpnRequest.ResponseTime.ToString() },
                { "resultCode", moMoIpnRequest.ResultCode.ToString() },
                { "transId", moMoIpnRequest.TransId.ToString() }
            };
            var signature = BuildSignature(parameters);

            // Validate signature
            var isValid = signature.Equals(moMoIpnRequest.Signature, StringComparison.OrdinalIgnoreCase);
            if (!isValid)
            {
                _logger.LogWarning("Invalid IPN signature: OrderId={OrderId}, Expected={Expected}, Received={Received}", 
                    moMoIpnRequest.OrderId, signature, moMoIpnRequest.Signature);
            }
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IPN signature");
            throw;
        }
    }

    public async Task<object?> QueryTransactionAsync(object request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Cast request to MoMoQueryTransactionRequest
            var moMoQueryTransactionRequest = request as MoMoQueryTransactionRequest ?? throw new ArgumentException("Invalid request type", nameof(request));
            
            // Build signature
            var parameters = new Dictionary<string, string>
            {
                { "accessKey", _accessKey },
                { "orderId", moMoQueryTransactionRequest.OrderId },
                { "partnerCode", moMoQueryTransactionRequest.PartnerCode },
                { "requestId", moMoQueryTransactionRequest.RequestId },
            };
            var signature = BuildSignature(parameters);
            
            // Call query transaction API
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync($"{_apiEndpoint}/query", moMoQueryTransactionRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<MoMoTransactionResultResponse>(
                options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }, 
                cancellationToken)
                ?? throw new InvalidOperationException("Failed to parse MoMo transaction result response");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying transaction");
            throw;
        }
    }
}
