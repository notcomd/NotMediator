namespace NotMediator;

public interface INotificationHandler<in TNotifications> where TNotifications : INotifications
{
    Task Handler(TNotifications notifications, CancellationToken cancellationToken = default);
}