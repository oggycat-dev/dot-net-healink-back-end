using SharedLibrary.Commons.Enums;

namespace NotificationService.Application.Commons.Interfaces;

public interface INotificationFactory
{
    INotificationService GetSender(NotificationChannelEnum channel);
}