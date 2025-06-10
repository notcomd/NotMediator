namespace NotMediator;

class GenericNotificationHandler(NotMediator mediator) : INotificationHandler<INotifications>
{

    public async Task Handler(INotifications notification, CancellationToken cancellationToken)
    {
        var notificationType = notification.GetType();
        mediator.GetOrCreateChannel(notificationType);
        await mediator.PublishAsync(notification, cancellationToken);
    }
}