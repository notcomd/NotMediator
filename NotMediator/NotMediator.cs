using System.Collections.Concurrent;
using System.Threading.Channels;

namespace NotMediator;

class NotMediator : INotMediator
{

    /// <summary>
    ///     请求处理器
    /// </summary>
    private readonly static ConcurrentDictionary<Type, object> _requestHandlers = new();

    /// <summary>
    ///     通知处理器
    /// </summary>
    private readonly static ConcurrentDictionary<Type, ConcurrentBag<object>> _notificationHandlers = new();

    /// <summary>
    ///     通知通道
    /// </summary>
    private readonly static ConcurrentDictionary<Type, Channel<INotifications>> _notificationChannels = new();

    /// <summary>
    ///     处理任务
    /// </summary>
    private readonly static ConcurrentDictionary<Type, Task> _processingTasks = new();

    /// <summary>
    ///     是否已释放
    /// </summary>
    private bool _disposed;

    public NotMediator()
    {
        // 注册一个特殊处理器用于捕获所有类型的通知
        RegisterNotificationHandler(new GenericNotificationHandler(this));
    }



    /// <summary>
    ///     发送请求
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {

        EnsureNotDisposed();
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        if (!_requestHandlers.TryGetValue(requestType, out var handlerObj))
        {
            throw new InvalidOperationException($"No handler registered for {requestType}");
        }

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handleMethod = handlerType.GetMethod("Handler");

        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handler for {requestType} does not implement Handle method correctly");
        }

        var result = handleMethod.Invoke(handlerObj, new object[]
        {
            request, cancellationToken
        });
        return await (result as Task<TResponse>);
    }



    /// <summary>
    ///     发布通知
    /// </summary>
    /// <param name="notification"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TNotification"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotifications
    {
        EnsureNotDisposed();
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);
        var channel = GetOrCreateChannel(notificationType);

        await channel.Writer.WriteAsync(notification, cancellationToken);
    }

    /// <summary>
    ///     等待所有处理任务完成
    /// </summary>
    public async Task CompleteAsync()
    {
        foreach (var channel in _notificationChannels.Values)
        {
            channel.Writer.Complete();
        }
        await Task.WhenAll(_processingTasks.Values);
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     确保对象没有被释放
    /// </summary>
    /// <param name="handler"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterRequestHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        EnsureNotDisposed();
        _requestHandlers[typeof(TRequest)] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    ///     注册通知处理器
    /// </summary>
    /// <param name="handler"></param>
    /// <typeparam name="TNotification"></typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterNotificationHandler<TNotification>(INotificationHandler<TNotification> handler)
        where TNotification : INotifications
    {
        EnsureNotDisposed();
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var handlers = _notificationHandlers.GetOrAdd(
            typeof(TNotification),
            _ => new ConcurrentBag<object>());

        handlers.Add(handler);
    }

    /// <summary>
    ///     获取或创建通知通道
    /// </summary>
    /// <param name="notificationType">  </param>
    /// <returns></returns>
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

    /// <summary>
    ///     处理通知
    /// </summary>
    /// <param name="notificationType">通知类型</param>
    /// <param name="channel">通知类型的channel</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <exception cref="AggregateException"></exception>
    private async Task ProcessNotificationsAsync(Type notificationType, Channel<INotifications> channel, CancellationToken cancellationToken)
    {
        await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (_notificationHandlers.TryGetValue(notification.GetType(), out var notificationHandler))
            {
                var exceptions = new List<Exception>();

                foreach (var handlerObj in notificationHandler)
                {
                    try
                    {
                        // 使用反射调用正确的处理方法
                        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
                        var handleMethod = handlerType.GetMethod("Handler");

                        if (handleMethod != null)
                        {
                            var task = (Task)handleMethod.Invoke(handlerObj, new object[]
                            {
                                notification, cancellationToken
                            })!;
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


            if (_notificationHandlers.TryGetValue(notificationType, out var handlers))
            {
                var exceptions = new List<Exception>();

                foreach (var handlerObj in handlers)
                {
                    try
                    {
                        // 使用反射调用正确的处理方法
                        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
                        var handleMethod = handlerType.GetMethod("Handler");

                        if (handleMethod != null)
                        {
                            var task = (Task)handleMethod.Invoke(handlerObj, new object[]
                            {
                                notification, cancellationToken
                            })!;
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

            _requestHandlers.Clear();
            _notificationHandlers.Clear();
            _notificationChannels.Clear();
            _processingTasks.Clear();
        }

        _disposed = true;
    }

    /// <summary>
    ///     确保对象没有被销毁
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NotMediator));
    }
}