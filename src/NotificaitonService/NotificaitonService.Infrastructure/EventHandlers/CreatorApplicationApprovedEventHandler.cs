using Microsoft.Extensions.Logging;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using NotificationService.Infrastructure.Helpers;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Commons.EventBus;
using SharedLibrary.Contracts.User.Events;

namespace NotificationService.Infrastructure.EventHandlers;

/// <summary>
/// Event handler to send notification when a creator application is approved
/// </summary>
public class CreatorApplicationApprovedEventHandler : IIntegrationEventHandler<CreatorApplicationApprovedEvent>
{
    private readonly INotificationFactory _notificationFactory;
    private readonly ILogger<CreatorApplicationApprovedEventHandler> _logger;

    public CreatorApplicationApprovedEventHandler(
        INotificationFactory notificationFactory,
        ILogger<CreatorApplicationApprovedEventHandler> logger)
    {
        _notificationFactory = notificationFactory;
        _logger = logger;
    }

    public async Task Handle(CreatorApplicationApprovedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Received CreatorApplicationApprovedEvent. ApplicationId: {ApplicationId}, UserId: {UserId}, Email: {Email}",
            @event.ApplicationId, @event.UserId, @event.UserEmail);

        try
        {
            // Get email notification service
            var notificationService = _notificationFactory.GetSender(NotificationChannelEnum.Email);

            // Build notification using template helper
            var notificationRequest = NotificationTemplateHelper.BuildCreatorApprovedNotification(
                email: @event.UserEmail,
                applicationId: @event.ApplicationId.ToString(),
                approvedAt: @event.ApprovedAt.ToString("dd/MM/yyyy HH:mm"),
                roleName: @event.BusinessRoleName,
                supportEmail: "support@healink.com",
                appName: "Healink"
            );

            // Create recipient info
            var recipient = new RecipientInfo
            {
                Email = @event.UserEmail,
                FullName = @event.UserEmail.Split('@')[0] // Use email prefix as fallback name
            };

            // Send email notification (fire and forget - non-critical)
            _ = Task.Run(async () =>
            {
                var result = await notificationService.SendNotificationAsync(notificationRequest, recipient);

                if (result.ChannelResults.Any(cr => cr.Success))
                {
                    _logger.LogInformation(
                        "Successfully sent creator approval notification. UserId: {UserId}, Email: {Email}",
                        @event.UserId, @event.UserEmail);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to send creator approval notification. UserId: {UserId}, Email: {Email}, Error: {Error}",
                        @event.UserId, @event.UserEmail,
                        result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing CreatorApplicationApprovedEvent. UserId: {UserId}, Email: {Email}",
                @event.UserId, @event.UserEmail);

            // Don't throw - notification failure shouldn't break the approval process
            // The application is already approved in UserService
        }
    }
}
