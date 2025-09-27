using System.Linq.Expressions;
using AuthService.Application.Commons.DTOs;
using AuthService.Application.Commons.Interfaces;
using AuthService.Application.Helpers;
using AuthService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Contracts.Auth;

namespace AuthService.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;
    private readonly IOtpCacheService _otpCacheService;
    private readonly string _PasswordEncryptionKey;

    public ResetPasswordCommandHandler(IIdentityService identityService,
     ILogger<ResetPasswordCommandHandler> logger, IOtpCacheService otpCacheService, IConfiguration configuration,
     IOutboxUnitOfWork outboxUnitOfWork)
    {
        _identityService = identityService;
        _logger = logger;
        _otpCacheService = otpCacheService;
        _PasswordEncryptionKey = configuration["PasswordEncryptionKey"] ?? throw new Exception("PasswordEncryptionKey is not set");
        _outboxUnitOfWork = outboxUnitOfWork;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        try
        {
            Expression<Func<AppUser, bool>> expression = command.Request.OtpSentChannel switch
            {
                NotificationChannelEnum.Email => x => x.Email == command.Request.Contact,
                NotificationChannelEnum.SMS => x => x.PhoneNumber == command.Request.Contact,
                _ => throw new ArgumentException("Invalid notification channel.")
            };
            var user = await _identityService.GetUserByFirstOrDefaultAsync(expression);
            if (user == null)
            {
                return Result.Failure("User not found", ErrorCodeEnum.NotFound);
            }
            //encrypt password
            var passwordEncrypt = PasswordCryptoHelper.Encrypt(command.Request.NewPassword, _PasswordEncryptionKey);

            //generate token for reset password
            var token = await _identityService.GeneratePasswordResetToken(user);

            // Create password reset data to store in cache
            var passwordResetData = new PasswordResetData // ✅ Sử dụng strongly-typed class
            {
                EncryptedPassword = passwordEncrypt,
                ResetToken = token
            };

            // Store password reset data in cache with a short expiration time (e.g., 15 minutes)
            var cacheResult = await _otpCacheService.GenerateAndStoreOtpAsync(
                command.Request.Contact,
                OtpTypeEnum.PasswordReset,
                passwordResetData,
                command.Request.OtpSentChannel);
            if (cacheResult.OtpCode == null)
            {
                _logger.LogError("Failed to store password reset data in cache for contact: {Contact}", command.Request.Contact);
                return Result.Failure("Failed to initiate password reset. Please try again.", ErrorCodeEnum.InternalError);
            }

            //todo: publish event to send otp to notification service
            var resetPasswordEvent = new ResetPasswordEvent
            {
                Contact = command.Request.Contact,
                Otp = cacheResult.OtpCode,
                OtpSentChannel = command.Request.OtpSentChannel,
                ExpiresInMinutes = cacheResult.ExpiresInMinutes,
            };

            await _outboxUnitOfWork.AddOutboxEventAsync(resetPasswordEvent);
            await _outboxUnitOfWork.SaveChangesWithOutboxAsync(cancellationToken);

            return Result.Success("Password reset initiated successfully. Please check your contact for further instructions.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while resetting password for user with email {Email}", command.Request.Contact);
            return Result.Failure("An error occurred while resetting password.", ErrorCodeEnum.InternalError);
        }
    }
}