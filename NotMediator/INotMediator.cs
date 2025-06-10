namespace NotMediator;

public interface INotMediator : IDisposable
{


    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotifications;

    Task CompleteAsync();
}