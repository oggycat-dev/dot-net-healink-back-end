using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commons.Enums;
using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Commons.Enums;
using SharedLibrary.Contracts.Notification;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>
/// Consumer for subscription activation notifications
/// Sends email/SMS to user when their subscription is activated
/// Fire-and-forget pattern - no response expected
/// </summary>
public class SubscriptionActivatedNotificationConsumer : IConsumer<SubscriptionActivatedNotificationEvent>
{
    private readonly INotificationFactory _notificationFactory;
    private readonly IUserStateCache _userStateCache;
    private readonly ILogger<SubscriptionActivatedNotificationConsumer> _logger;

    public SubscriptionActivatedNotificationConsumer(
        INotificationFactory notificationFactory,
        IUserStateCache userStateCache,
        ILogger<SubscriptionActivatedNotificationConsumer> logger)
    {
        _notificationFactory = notificationFactory;
        _userStateCache = userStateCache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionActivatedNotificationEvent> context)
    {
        try
        {
            var message = context.Message;

            _logger.LogInformation(
                "Processing subscription activation notification: SubscriptionId={SubscriptionId}, UserId={UserId}",
                message.SubscriptionId, message.UserId);

            // ✅ Step 1: Get user info from cache (UserId from UpdatedBy in ActivateSubscription command)
            var userState = await _userStateCache.GetUserStateAsync(message.UserId);
            if (userState == null)
            {
                _logger.LogWarning(
                    "User state not found in cache for UserId={UserId}, SubscriptionId={SubscriptionId}. Skipping notification.",
                    message.UserId, message.SubscriptionId);
                return; // Skip notification if user not in cache
            }

            if (string.IsNullOrWhiteSpace(userState.Email))
            {
                _logger.LogWarning(
                    "User email is empty in cache for UserId={UserId}, SubscriptionId={SubscriptionId}. Skipping notification.",
                    message.UserId, message.SubscriptionId);
                return; // Skip notification if email not available
            }

            _logger.LogInformation(
                "User email retrieved from cache: UserId={UserId}, Email={Email}",
                message.UserId, userState.Email);

            // ✅ Step 2: Get email notification service
            var notificationService = _notificationFactory.GetSender(NotificationChannelEnum.Email);

            // ✅ Step 3: Prepare notification request
            var notificationRequest = new NotificationRequest
            {
                To = userState.Email, // ✅ Email from cache
                Template = NotificationTemplateEnums.SubscriptionActivated,
                Priority = NotificationPriorityEnum.Normal,
                TemplateData = new Dictionary<string, object>
                {
                    ["subscriptionId"] = message.SubscriptionId,
                    ["subscriptionPlanName"] = message.SubscriptionPlanDisplayName ?? message.SubscriptionPlanName,
                    ["amount"] = message.Amount.ToString("N2"),
                    ["currency"] = message.Currency,
                    ["activatedAt"] = message.ActivatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["paymentProvider"] = message.PaymentProvider,
                    ["transactionId"] = message.TransactionId
                }
            };

            // ✅ Step 4: Recipient info (email from cache)
            var recipientInfo = new RecipientInfo
            {
                UserId = message.UserProfileId,
                Email = userState.Email, // ✅ Set email from cache
                FullName = userState.Email.Split('@')[0] // Use email prefix as name if not available
            };

            // ✅ Step 5: Send notification
            var result = await notificationService.SendNotificationAsync(notificationRequest, recipientInfo);

            if (result.ChannelResults.Any(x => x.Success))
            {
                _logger.LogInformation(
                    "Subscription activation notification sent successfully: SubscriptionId={SubscriptionId}",
                    message.SubscriptionId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send subscription activation notification: SubscriptionId={SubscriptionId}, Error={Error}",
                    message.SubscriptionId,
                    result.ChannelResults.FirstOrDefault(cr => !cr.Success)?.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - fire-and-forget pattern
            // Notification failure should not affect subscription activation
            _logger.LogError(ex,
                "Error sending subscription activation notification: SubscriptionId={SubscriptionId}",
                context.Message.SubscriptionId);
        }
    }
}

