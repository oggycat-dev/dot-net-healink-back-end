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
        _passwordEncryptionKey = configuration["PasswordEncryption:Key"] ?? throw new ArgumentNullException("PasswordEncryption:Key is not configured");
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

            // Create correlation ID for saga
            var correlationId = Guid.NewGuid();
            
            // Create request with correlation ID for OTP cache
            var requestWithCorrelation = new 
            {
                OriginalRequest = command.Request,
                CorrelationId = correlationId
            };

            // Generate and store OTP with rate limiting check
            var otpResult = await _otpCacheService.GenerateAndStoreOtpAsync(
                    contact,
                    OtpTypeEnum.Registration,
                    requestWithCorrelation,
                    channel);
            
            var otpCode = otpResult.OtpCode;
            var expiresInMinutes = otpResult.ExpiresInMinutes;
                    
            //3. Start Registration Saga
            await _publishEndpoint.Publish<RegistrationStarted>(new
            {
                CorrelationId = correlationId,
                Email = command.Request.Email,
                EncryptedPassword = PasswordCryptoHelper.Encrypt(command.Request.Password, _passwordEncryptionKey),
                FullName = command.Request.FullName,
                PhoneNumber = command.Request.PhoneNumber,
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