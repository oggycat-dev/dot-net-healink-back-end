using NotificationService.Application.Commons.Models;

namespace NotificationService.Application.Commons.Interfaces;

public interface INotificationService
{
    Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient);
    Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients);
}