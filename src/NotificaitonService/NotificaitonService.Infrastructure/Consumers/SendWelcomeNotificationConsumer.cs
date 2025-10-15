using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using SharedLibrary.Contracts.User.Saga;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý SendWelcomeNotification event từ Registration Saga
/// </summary>
public class SendWelcomeNotificationConsumer : IConsumer<SendWelcomeNotification>
{
    private readonly INotificationFactory _notificationFactory;
    private readonly ILogger<SendWelcomeNotificationConsumer> _logger;

    public SendWelcomeNotificationConsumer(
        INotificationFactory notificationFactory,
        ILogger<SendWelcomeNotificationConsumer> logger)
    {
        _notificationFactory = notificationFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendWelcomeNotification> context)
    {
        var message = context.Message;

        try
        {
            _logger.LogInformation("Processing SendWelcomeNotification for email: {Email}, CorrelationId: {CorrelationId}",
                message.Email, message.CorrelationId);

            // Get email notification service
            var notificationService = _notificationFactory.GetSender(SharedLibrary.Commons.Enums.NotificationChannelEnum.Email);

            // Create notification request
            var notificationRequest = new NotificationRequest
            {
                To = message.Email,
                Template = NotificationTemplateEnums.Welcome,
                Priority = NotificationPriorityEnum.Normal,
                TemplateData = new Dictionary<string, object>
                {
                    ["fullName"] = message.FullName,
                    ["email"] = message.Email
                }
            };

            // Create recipient info
            var recipient = new RecipientInfo
            {
                Email = message.Email,
                FullName = message.FullName
            };

            // Send welcome notification
            //current, send by task run, fire and forget
            _ = Task.Run(async () =>
            {
            var result = await notificationService.SendNotificationAsync(notificationRequest, recipient);

            // if (result.ChannelResults.Any(cr => cr.Success))
            // {
            //     _logger.LogInformation("Welcome notification sent successfully for email: {Email}, CorrelationId: {CorrelationId}",
            //         message.Email, message.CorrelationId);
            // }
            // else
            // {
            //     _logger.LogWarning("Failed to send welcome notification for email: {Email}, CorrelationId: {CorrelationId}, Error: {Error}",
            //         message.Email, message.CorrelationId, result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage);

            //     // Welcome notification failure is not critical, don't fail the saga
            // }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendWelcomeNotification for email: {Email}, CorrelationId: {CorrelationId}",
                message.Email, message.CorrelationId);

            // Welcome notification failure is not critical, don't fail the saga
        }
    }
}