using AuthService.Application.Commons.Interfaces;
using AuthService.Application.Helpers;
using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Contracts.User.Saga;

namespace AuthService.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IOtpCacheService _otpCacheService;
    private readonly ILogger<RegisterCommandHandler> _logger;
    private readonly string _passwordEncryptionKey;

    public RegisterCommandHandler(IPublishEndpoint publishEndpoint, IOtpCacheService otpCacheService, ILogger<RegisterCommandHandler> logger, IConfiguration configuration)
    {
        _publishEndpoint = publishEndpoint;
        _otpCacheService = otpCacheService;
        _passwordEncryptionKey = configuration["PasswordEncryptionKey"] ?? throw new ArgumentNullException("PasswordEncryptionKey is not configured");
        _logger = logger;
    }

    public async Task<Result> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        try
        {
            // Determine contact based on channel
            var channel = request.OtpSentChannel ?? NotificationChannelEnum.Email;
            var contact = channel == NotificationChannelEnum.Email ?
                request.Email : request.PhoneNumber;

            if (string.IsNullOrEmpty(contact))
            {
                return Result.Failure("Contact information is required", ErrorCodeEnum.ValidationFailed);
            }

            // Check if there's already an active registration OTP for this contact
            var existingOtpData = await _otpCacheService.GetOtpDataAsync(contact, OtpTypeEnum.Registration);
            if (existingOtpData != null)
            {
                _logger.LogWarning("Active registration OTP already exists for contact: {Contact}. Please wait or verify existing OTP.", contact);
                return Result.Failure("Registration already in progress. Please check your email/phone for existing OTP or wait a few minutes before trying again.", ErrorCodeEnum.ResourceConflict);
            }

            // CRITICAL: Additional safety check - ensure unique correlation ID and avoid race conditions
            // Log the attempt with timestamp for monitoring duplicate requests
            _logger.LogInformation("Registration attempt for {Email} at {Timestamp}", request.Email, DateTime.UtcNow);

            // Create correlation ID for saga
            var correlationId = Guid.NewGuid();
            _logger.LogInformation("Starting registration for {Email} with new CorrelationId {CorrelationId}", request.Email, correlationId);
            
            // Create strongly-typed correlation data for OTP cache
            var correlationData = new RegistrationCorrelationData(
                correlationId, 
                command.Request, 
                PasswordCryptoHelper.Encrypt(command.Request.Password, _passwordEncryptionKey),
                channel);

            // Generate and store OTP with rate limiting check
            var otpResult = await _otpCacheService.GenerateAndStoreOtpAsync(
                    contact,
                    OtpTypeEnum.Registration,
                    correlationData,
                    channel);
            
            var otpCode = otpResult.OtpCode;
            var expiresInMinutes = otpResult.ExpiresInMinutes;
                    
            //3. Start Registration Saga
            await _publishEndpoint.Publish<RegistrationStarted>(new
            {
                CorrelationId = correlationId,
                Email = correlationData.Email,
                EncryptedPassword = correlationData.EncryptedPassword,
                FullName = correlationData.FullName,
                PhoneNumber = correlationData.PhoneNumber,
                OtpCode = otpCode,
                Channel = channel,
                ExpiresInMinutes = expiresInMinutes
            }, cancellationToken);
            
            return Result.Success("User registration started. Please check your email/phone for OTP verification.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while registering user: {ErrorMetpssage}", ex);
            return Result.Failure("An error occurred while registering.", ErrorCodeEnum.InternalError);
        }
    }
}