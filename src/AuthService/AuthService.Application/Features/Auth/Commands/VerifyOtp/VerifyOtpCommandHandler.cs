using System.Text.Json;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Contracts.User.Saga;

namespace AuthService.Application.Features.Auth.Commands.VerifyOtp;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOtpCacheService _otpCacheService;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        IPublishEndpoint publishEndpoint,
        IOtpCacheService otpCacheService,
        ILogger<VerifyOtpCommandHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _otpCacheService = otpCacheService;
        _logger = logger;
    }

    public async Task<Result> Handle(VerifyOtpCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        
        try
        {
            _logger.LogInformation("Verifying OTP for contact: {Contact}", request.Contact);
            
            // Verify OTP using existing cache service
            var verificationResult = await _otpCacheService.VerifyOtpAsync(
                request.Contact,
                request.OtpCode,
                OtpTypeEnum.Registration,
                request.Channel);

            if (!verificationResult.Success)
            {
                _logger.LogWarning("OTP verification failed for contact: {Contact}. Reason: {Reason}", 
                    request.Contact, verificationResult.Message);
                return Result.Failure(verificationResult.Message, ErrorCodeEnum.ValidationFailed);
            }

            // Get correlation ID from cached OTP data (should be stored in userData)
            var otpData = await _otpCacheService.GetOtpDataAsync(request.Contact, OtpTypeEnum.Registration);
            if (otpData?.userData == null)
            {
                _logger.LogError("Cannot find correlation ID for contact: {Contact}", request.Contact);
                return Result.Failure("Registration session not found. Please restart registration.", ErrorCodeEnum.NotFound);
            }

            // Extract correlation ID from userData (assuming it was stored during registration)
            // We need to modify the OTP cache to store saga correlation ID
            // For now, we'll search for active saga by contact email
            var correlationId = ExtractCorrelationId(otpData.userData, request.Contact);
            if (correlationId == Guid.Empty)
            {
                _logger.LogError("Invalid correlation ID extracted for contact: {Contact}", request.Contact);
                return Result.Failure("Invalid registration session. Please restart registration.", ErrorCodeEnum.ValidationFailed);
            }

            // Publish OtpVerified event to continue saga
            await _publishEndpoint.Publish<OtpVerified>(new
            {
                CorrelationId = correlationId,
                Contact = request.Contact,
                Type = OtpTypeEnum.Registration,
                VerifiedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("OTP verified successfully for contact: {Contact}", request.Contact);
            return Result.Success("OTP verified successfully. Creating your account...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying OTP for contact: {Contact}", request.Contact);
            return Result.Failure("An error occurred while verifying OTP.", ErrorCodeEnum.InternalError);
        }
    }

    private Guid ExtractCorrelationId(object userData, string contact)
    {
        try
        {
            // Try to extract correlation ID from userData
            if (userData != null)
            {
                // Convert userData to JsonElement first
                var json = JsonSerializer.Serialize(userData);
                var jsonDocument = JsonDocument.Parse(json);

                // Check if userData has CorrelationId property
                if (jsonDocument.RootElement.TryGetProperty("CorrelationId", out var correlationIdElement))
                {
                    if (correlationIdElement.TryGetGuid(out var correlationId))
                    {
                        return correlationId;
                    }
                }

                // Check if userData has OriginalRequest wrapper
                if (jsonDocument.RootElement.TryGetProperty("CorrelationId", out var wrapperCorrelationId))
                {
                    if (wrapperCorrelationId.TryGetGuid(out var wrappedId))
                    {
                        return wrappedId;
                    }
                }
            }
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract correlation ID from userData for contact: {Contact}", contact);
            return Guid.Empty;
        }
    }
}
