using System.Linq.Expressions;
using System.Text.Json;
using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using AuthService.Application.Helpers;
using AuthService.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
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
    private readonly string _PasswordEncryptionKey;
    private readonly IIdentityService _identityService;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        IPublishEndpoint publishEndpoint,
        IOtpCacheService otpCacheService,
        ILogger<VerifyOtpCommandHandler> logger,
        IConfiguration configuration,
        IIdentityService identityService)
    {
        _publishEndpoint = publishEndpoint;
        _otpCacheService = otpCacheService;
        _logger = logger;
        _PasswordEncryptionKey = configuration.GetValue<string>("PasswordEncryptionKey") ?? throw new Exception("PasswordEncryptionKey is not set");
        _identityService = identityService;
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
                command.Request.OtpType,
                request.OtpSentChannel);

            if (!verificationResult.Success)
            {
                _logger.LogWarning("OTP verification failed for contact: {Contact}. Reason: {Reason}", 
                    request.Contact, verificationResult.Message);
                return Result.Failure(verificationResult.Message, ErrorCodeEnum.ValidationFailed);
            }
            
            var userData = verificationResult.UserData;

            var result = request.OtpType switch
            {
                OtpTypeEnum.Registration => await HandleVerfiyOtpForRegister(request, cancellationToken, userData),
                OtpTypeEnum.PasswordReset => await HandleVerfiyOtpForResetPassword(request, cancellationToken, userData),
                _ => Result.Failure("Invalid OTP type.", ErrorCodeEnum.ValidationFailed)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying OTP for contact: {Contact}", request.Contact);
            return Result.Failure("An error occurred while verifying OTP.", ErrorCodeEnum.InternalError);
        }
    }

    private async Task<Result> HandleVerfiyOtpForRegister(VerifyOtpRequest request, CancellationToken cancellationToken, object? userData)
    {
        // Get correlation ID from cached OTP data (should be stored in userData)
            var otpData = await _otpCacheService.GetOtpDataAsync(request.Contact, OtpTypeEnum.Registration);
            if (otpData?.userData == null)
            {
                _logger.LogError("Cannot find correlation ID for contact: {Contact}", request.Contact);
                return Result.Failure("Registration session not found. Please restart registration.", ErrorCodeEnum.NotFound);
            }

            // Extract correlation data - now strongly typed!
            var correlationData = ExtractCorrelationData(otpData.userData, request.Contact);
            if (correlationData?.CorrelationId == Guid.Empty || correlationData == null)
            {
                _logger.LogError("Invalid correlation ID extracted for contact: {Contact}", request.Contact);
                return Result.Failure("Invalid registration session. Please restart registration.", ErrorCodeEnum.ValidationFailed);
            }

            // Publish OtpVerified event to continue saga
            await _publishEndpoint.Publish<OtpVerified>(new
            {
                CorrelationId = correlationData.CorrelationId,
                Contact = request.Contact,
                Type = OtpTypeEnum.Registration,
                VerifiedAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("OTP verified successfully for contact: {Contact}", request.Contact);
            return Result.Success("OTP verified successfully. Creating your account...");
    }

    private async Task<Result> HandleVerfiyOtpForResetPassword(VerifyOtpRequest request, CancellationToken cancellationToken, object? userData)
    {
        // Extract password reset data from cached userData
         var json = JsonSerializer.Serialize(userData);
        var passwordResetData = JsonSerializer.Deserialize<PasswordResetData>(json);
        
        if (passwordResetData == null)
        {
            _logger.LogError("Failed to deserialize password reset data for contact: {Contact}", request.Contact);
            return Result.Failure("Invalid password reset session. Please restart password reset.", ErrorCodeEnum.ValidationFailed);
        }

        var encryptedPassword = passwordResetData.EncryptedPassword;
        var resetToken = passwordResetData.ResetToken;

        // Decrypt the password to get the plain text for Identity's ResetPasswordAsync
        var plainPassword = PasswordCryptoHelper.Decrypt(encryptedPassword, _PasswordEncryptionKey);

        Expression<Func<AppUser, bool>> expression = request.OtpSentChannel switch
        {
            NotificationChannelEnum.Email => x => x.Email == request.Contact,
            NotificationChannelEnum.SMS => x => x.PhoneNumber == request.Contact,
            _ => throw new ArgumentException("Invalid notification channel.")
        };

        var updateResult = await _identityService.ResetUserPasswordAsync(expression, resetToken, plainPassword);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description).ToList();
            return Result.Failure("Failed to update user", ErrorCodeEnum.InternalError, errors);
        }

        return Result.Success("Password reset successfully.");
    }

    private RegistrationCorrelationData? ExtractCorrelationData(object userData, string contact)
    {
        try
        {
            // Direct deserialization for strongly-typed object
            if (userData != null)
            {
                var json = JsonSerializer.Serialize(userData);
                var correlationData = JsonSerializer.Deserialize<RegistrationCorrelationData>(json);

                if (correlationData?.CorrelationId != Guid.Empty)
                {
                    return correlationData;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract correlation data from userData for contact: {Contact}", contact);
            return null;
        }
    }
}
