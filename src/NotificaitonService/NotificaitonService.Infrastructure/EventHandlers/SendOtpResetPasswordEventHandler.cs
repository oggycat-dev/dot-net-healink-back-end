using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.Auth;

namespace NotificationService.Infrastructure.EventHandlers;

public class SendOtpResetPasswordEventHandler : IIntegrationEventHandler<ResetPasswordEvent>
{
    private readonly INotificationFactory _notificationFactory;
    private readonly ILogger<SendOtpResetPasswordEventHandler> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromMinutes(1);

    public SendOtpResetPasswordEventHandler(
        INotificationFactory notificationFactory,
        ILogger<SendOtpResetPasswordEventHandler> logger,
        IMemoryCache cache)
    {
        _notificationFactory = notificationFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task Handle(ResetPasswordEvent @event, CancellationToken cancellationToken)
    {
        //1. Check for duplicate processing using idempotency
        var idempotencyKey = $"reset_password_notification_{@event.Id}_{@event.Contact}";
        try
        {
            if (_cache.TryGetValue(idempotencyKey, out _))
            {
                _logger.LogInformation("Duplicate ResetPasswordNotification detected for contact: {Contact}, EventId: {EventId}. Skipping processing.",
                    @event.Contact, @event.Id);
                return;
            }

            //2. Mark as processing to prevent duplicates
            //key: idempotencyKey, value: DateTime.UtcNow, expiration: IdempotencyWindow
            _cache.Set(idempotencyKey, DateTime.UtcNow, IdempotencyWindow);

            //3. Get notification service based on channel
            var notificationService = _notificationFactory.GetSender(@event.OtpSentChannel);
            //3. Init notification request
            var notificationRequest = new NotificationRequest
            {
                To = @event.Contact,
                Template = NotificationTemplateEnums.Otp,
                Priority = NotificationPriorityEnum.High,
                TemplateData = new Dictionary<string, object>
                {
                    ["otpCode"] = @event.Otp,
                    ["otpType"] = OtpTypeEnum.PasswordReset.ToString(),
                    ["expiresInMinutes"] = @event.ExpiresInMinutes.ToString(),
                }
            };

            //4. Init recipient info
            var recipientInfo = new RecipientInfo
            {
                Email = @event.OtpSentChannel == NotificationChannelEnum.Email ? @event.Contact : null,
                PhoneNumber = @event.OtpSentChannel == NotificationChannelEnum.SMS ? @event.Contact : null,
            };

            //5. Send notification and await result
            var result = await notificationService.SendNotificationAsync(notificationRequest, recipientInfo);

            if (!result.ChannelResults.Any(x => x.Success))
            {
                _logger.LogInformation("ResetPasswordNotification sent successfully to {Contact}, EventId: {EventId}",
                    @event.Contact, @event.Id);
            }
            else
            {
                _logger.LogError("ResetPasswordNotification failed to send to {Contact}, EventId: {EventId}, Error: {Error}",
                        @event.Contact, @event.Id, result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending ResetPasswordNotification to {Contact}, EventId: {EventId}",
                @event.Contact, @event.Id);
        }
    }
}