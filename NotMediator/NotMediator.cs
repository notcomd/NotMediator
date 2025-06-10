using System.Collections.Concurrent;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

class NotMediator : INotMediator
{
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, Channel<INotifications>> _notificationChannels = new();
    private readonly ConcurrentDictionary<Type, Task> _processingTasks = new();
    private bool _disposed;

    public NotMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
      
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler registered for {requestType}");

        var handleMethod = handlerType.GetMethod("Handler");
        if (handleMethod == null)
            throw new InvalidOperationException($"Handler for {requestType} does not implement Handle method correctly");

        var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });
        return await (result as Task<TResponse>);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotifications
    {
        EnsureNotDisposed();
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);
        var channel = GetOrCreateChannel(notificationType);

        await channel.Writer.WriteAsync(notification, cancellationToken);
    }

    public async Task CompleteAsync()
    {
        foreach (var channel in _notificationChannels.Values)
        {
            channel.Writer.Complete();
        }
        await Task.WhenAll(_processingTasks.Values);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Channel<INotifications> GetOrCreateChannel(Type notificationType)
    {
        return _notificationChannels.GetOrAdd(
            notificationType,
            type =>
            {
                var channel = Channel.CreateUnbounded<INotifications>();
                var task = ProcessNotificationsAsync(type, channel, CancellationToken.None);
                _processingTasks[type] = task;
                return channel;
            });
    }

    private async Task ProcessNotificationsAsync(Type notificationType, Channel<INotifications> channel, CancellationToken cancellationToken)
    {
        await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = _serviceProvider.GetServices(handlerType);

            var exceptions = new List<Exception>();
            foreach (var handler in handlers)
            {
                try
                {
                    var handleMethod = handlerType.GetMethod("Handler");
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Any())
            {
                throw new AggregateException($"One or more exceptions occurred while processing {notificationType}", exceptions);
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var channel in _notificationChannels.Values)
            {
                channel.Writer.Complete();
            }
            try
            {
                Task.WaitAll(_processingTasks.Values.ToArray());
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("One or more exceptions occurred during disposal:");
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine($"  {innerEx.Message}");
                }
            }

            _notificationChannels.Clear();
            _processingTasks.Clear();
        }

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NotMediator));
    }
}
