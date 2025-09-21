using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using SharedLibrary.Contracts.User.Saga;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý SendOtpNotification event từ Registration Saga
/// </summary>
public class SendOtpNotificationConsumer : IConsumer<SendOtpNotification>
{
    private readonly INotificationFactory _notificationFactory;
    private readonly ILogger<SendOtpNotificationConsumer> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromMinutes(10);

    public SendOtpNotificationConsumer(
        INotificationFactory notificationFactory,
        ILogger<SendOtpNotificationConsumer> logger,
        IMemoryCache cache)
    {
        _notificationFactory = notificationFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<SendOtpNotification> context)
    {
        var message = context.Message;
        var idempotencyKey = $"otp_notification_{message.CorrelationId}_{message.Contact}";
        
        try
        {
            // Check for duplicate processing using idempotency
            if (_cache.TryGetValue(idempotencyKey, out _))
            {
                _logger.LogInformation("Duplicate SendOtpNotification detected for contact: {Contact}, CorrelationId: {CorrelationId}. Skipping processing.", 
                    message.Contact, message.CorrelationId);
                
                // Publish success response as this was already processed
                await context.Publish<OtpSent>(new
                {
                    CorrelationId = message.CorrelationId,
                    Success = true,
                    ErrorMessage = (string?)null,
                    SentAt = DateTime.UtcNow
                });
                return;
            }
            
            // Mark as processing to prevent duplicates
            _cache.Set(idempotencyKey, DateTime.UtcNow, IdempotencyWindow);
            
            _logger.LogInformation("Processing SendOtpNotification for contact: {Contact}, CorrelationId: {CorrelationId}", 
                message.Contact, message.CorrelationId);

            // Get notification service based on channel
            var notificationService = _notificationFactory.GetSender(message.Channel);
            
            // Create notification request
            var notificationRequest = new NotificationRequest
            {
                To = message.Contact,
                Template = NotificationTemplateEnums.Otp,
                Priority = NotificationPriorityEnum.High,
                TemplateData = new Dictionary<string, object>
                {
                    ["otpCode"] = message.OtpCode,
                    ["otpType"] = message.OtpType.ToString(),
                    ["fullName"] = message.FullName,
                    ["expirationMinutes"] = message.ExpiresInMinutes.ToString()
                }
            };

            // Create recipient info
            var recipient = new RecipientInfo
            {
                Email = message.Channel == SharedLibrary.Commons.Enums.NotificationChannelEnum.Email ? message.Contact : null,
                PhoneNumber = message.Channel == SharedLibrary.Commons.Enums.NotificationChannelEnum.Firebase ? message.Contact : null,
                FullName = message.FullName
            };

            // Send notification
            var result = await notificationService.SendNotificationAsync(notificationRequest, recipient);

            if (result.ChannelResults.Any(cr => cr.Success))
            {
                _logger.LogInformation("OTP notification sent successfully for contact: {Contact}, CorrelationId: {CorrelationId}", 
                    message.Contact, message.CorrelationId);

                // Publish success response
                await context.Publish<OtpSent>(new
                {
                    CorrelationId = message.CorrelationId,
                    Success = true,
                    ErrorMessage = (string?)null,
                    SentAt = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Failed to send OTP notification for contact: {Contact}, CorrelationId: {CorrelationId}, Error: {Error}", 
                    message.Contact, message.CorrelationId, result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage);

                // Publish failure response
                await context.Publish<OtpSent>(new
                {
                    CorrelationId = message.CorrelationId,
                    Success = false,
                    ErrorMessage = result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage ?? "Failed to send notification",
                    SentAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendOtpNotification for contact: {Contact}, CorrelationId: {CorrelationId}", 
                message.Contact, message.CorrelationId);

            // Publish failure response
            await context.Publish<OtpSent>(new
            {
                CorrelationId = message.CorrelationId,
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            });
        }
    }
}