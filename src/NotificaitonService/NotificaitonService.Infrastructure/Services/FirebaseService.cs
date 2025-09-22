using NotificationService.Application.Commons.Interfaces;
using NotificationService.Application.Commons.Models;

namespace NotificationService.Infrastructure.Services;

public class FirebaseService : IFirebaseService
{
    public Task<List<NotificationSendResult>> SendMultiCastAsync(NotificationRequest message, List<RecipientInfo> recipients)
    {
        throw new NotImplementedException();
    }

    public Task<NotificationSendResult> SendNotificationAsync(NotificationRequest message, RecipientInfo recipient)
    {
        throw new NotImplementedException();
    }
}