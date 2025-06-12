using System.Collections.Concurrent;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

class NotMediator : INotMediator
{
    /// <summary>
    /// 服务提供者，用于获取依赖项。
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 存储通知通道的字典，键为通知类型，值为对应的通道。
    /// </summary>
    private readonly ConcurrentDictionary<Type, Channel<INotifications>> _notificationChannels = new();
    /// <summary>
    /// 存储处理任务的字典，键为通知类型，值为对应的处理任务。
    /// </summary>
    private readonly ConcurrentDictionary<Type, Task> _processingTasks = new();
    /// <summary>
    /// 标记是否已释放资源。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// NotMediator 构造函数，接受一个 <see cref="IServiceProvider"/> 实例用于依赖注入。
    /// </summary>
    /// <param name="serviceProvider">服务实例</param>
    /// <exception cref="ArgumentNullException">
    /// 如果服务实例为空，则抛出此异常。
    /// </exception>
    public NotMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 发送请求并返回响应，支持管道（Pipeline）行为拦截。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    /// <param name="request">实现 <see cref="IRequest{TResponse}"/> 的请求对象。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>异步返回请求的响应结果。</returns>
    /// <exception cref="ArgumentNullException">请求参数为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">未找到对应的请求处理器或处理器未正确实现 Handle 方法时抛出。</exception>
    /// <remarks>
    /// 此方法会自动查找并执行与请求类型匹配的 <see cref="IRequestHandler{TRequest, TResponse}"/>，
    /// 并按注册顺序依次执行所有 <see cref="IPipelineBehavior{TRequest, TResponse}"/> 管道行为。
    /// 管道行为可在请求处理前后插入自定义逻辑（如日志、验证、事务等）。
    /// </remarks>
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
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType);
        Func<Task<TResponse>> HandlerDelegate = async () =>
        {
            var result = handleMethod.Invoke(handler, new object[] { request, cancellationToken });
            return await (Task<TResponse>)result!;
        };

        foreach (var behavior in behaviors)
        {
            var next = HandlerDelegate;
            HandlerDelegate = async () =>
            {
                var method = behaviorType.GetMethod("Handler");
                if (method is null) throw new InvalidOperationException($"Behavior {request} does not implement Handler method correctly");
                return await (Task<TResponse>)method.Invoke(behavior, new object[] { request, next, cancellationToken })!;
            };
        }
        return await HandlerDelegate();
    }

    /// <summary>
    /// 发布通知到对应的处理器。
    /// </summary>
    /// <typeparam name="TNotification">发布通知类型，该类型限制为INotifications</typeparam>
    /// <param name="notification">
    /// 继承了INotifications接口类型
    /// </param>
    /// <param name="cancellationToken">
    /// 可选的取消令牌，用于取消操作。
    /// </param>
    /// <returns>
    /// 无返回值。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 当通知参数为 null 时抛出此异常。
    /// </exception>
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
    /// 完成所有通知处理任务并释放资源。
    /// </summary>
    /// <returns>
    /// 无返回值。
    /// </returns>
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

    /// <summary>
    /// 获取或创建一个通知通道。
    /// </summary>
    /// <param name="notificationType">
    /// 通知类型。
    /// </param>
    /// <returns>
    /// 返回一个 <see cref="Channel{T}"/> 实例，用于处理指定类型的通知。
    /// </returns>
    private Channel<INotifications> GetOrCreateChannel(Type notificationType)
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
    /// 处理通知的异步方法。
    /// </summary>
    /// <param name="notificationType">通知类型</param>
    /// <param name="channel">
    /// channel 通道，用于读取通知。
    /// </param>
    /// <param name="cancellationToken">
    /// 可选的取消令牌，用于取消操作。
    /// </param>
    /// <returns>
    /// 无返回值。
    /// </returns>
    /// <exception cref="AggregateException">
    /// 当处理通知时发生一个或多个异常时抛出此异常。
    /// </exception>
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

    /// <summary>
    /// 释放资源。
    /// </summary>
    /// <param name="disposing">
    /// boolean 值，指示是否正在释放托管资源。
    /// </param>
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

    /// <summary>
    /// 确保 NotMediator 未被释放。
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// 当 NotMediator 已被释放时抛出此异常。
    /// </exception>
    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NotMediator));
    }
}
