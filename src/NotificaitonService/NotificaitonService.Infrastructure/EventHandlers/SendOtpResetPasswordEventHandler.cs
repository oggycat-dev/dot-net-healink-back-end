using MediatR;
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

    public SendOtpResetPasswordEventHandler(
        INotificationFactory notificationFactory,
        ILogger<SendOtpResetPasswordEventHandler> logger)
    {
        _notificationFactory = notificationFactory;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing ResetPasswordNotification for contact: {Contact}, EventId: {EventId}",
                @event.Contact, @event.Id);

            //1. Get notification service based on channel
            var notificationService = _notificationFactory.GetSender(@event.OtpSentChannel);
            
            //2. Init notification request
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

            //3. Init recipient info
            var recipientInfo = new RecipientInfo
            {
                Email = @event.OtpSentChannel == NotificationChannelEnum.Email ? @event.Contact : null,
                PhoneNumber = @event.OtpSentChannel == NotificationChannelEnum.SMS ? @event.Contact : null,
            };

            //4. Send notification and await result
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