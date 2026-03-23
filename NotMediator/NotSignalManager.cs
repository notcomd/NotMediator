using System.Reflection;

namespace NotMediator;

/// <summary>
/// 信号管理器实现
/// </summary>
public sealed class NotSignalManager : INotSignalManager
{
    private readonly Dictionary<string, List<Delegate>> _signalHandlers = new();
    private readonly List<Delegate> _globalListeners = new();
    private readonly Dictionary<string, List<Delegate>> _signalListeners = new();
    private readonly List<IGlobalSignalListener> _registeredGlobalListeners = new();
    
    /// <summary>
    /// 信号触发事件
    /// </summary>
    public event EventHandler<SignalEventArgs>? SignalEmitted;
    
    /// <summary>
    /// 绑定信号处理器
    /// </summary>
    public void Connect(string signalName, Delegate handler)
    {
        if (string.IsNullOrEmpty(signalName) || handler is null)
        {
            throw new ArgumentNullException($"参数不能为空！{nameof(signalName)} and {nameof(handler)}");
        }

        if (!_signalHandlers.TryGetValue(signalName, out var handlers))
        {
            handlers = new List<Delegate>();
            _signalHandlers[signalName] = handlers;
        }

        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
        }
    }
    
    /// <summary>
    /// 解绑信号处理器
    /// </summary>
    public void Disconnect(string signalName, Delegate handler)
    {
        if (string.IsNullOrEmpty(signalName) || handler is null)
        {
            return;
        }

        if (_signalHandlers.TryGetValue(signalName, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
    
    /// <summary>
    /// 发送信号（无发送者）
    /// </summary>
    public void Emit(string signalName, params object[] signalData)
    {
        Emit(signalName, null, signalData);
    }
    
    /// <summary>
    /// 发送信号（带发送者）
    /// </summary>
    public async void Emit(string signalName, object? sender, params object[] signalData)
    {
        var eventArgs = new SignalEventArgs(signalName, signalData, sender);
        
        // 1. 触发 SignalEmitted 事件
        OnSignalEmitted(eventArgs);
        
        // 2. 异步调用已注册的全局监听器（不阻塞主流程）
        _ = InvokeRegisteredGlobalListenersAsync(sender, eventArgs);
        
        // 3. 同步调用委托类型的全局监听器
        InvokeSignalListeners(signalName, eventArgs);
        
        // 4. 调用信号处理器
        if (!_signalHandlers.TryGetValue(signalName, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler.DynamicInvoke(signalData);
            }
            catch (TargetParameterCountException)
            {
                Console.WriteLine($"参数数量错误！{signalName}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"发生错误：无法调用信号！{signalName}:{e.Message}");
                throw;
            }
        }
    }
    
    /// <summary>
    /// 添加全局监听器
    /// </summary>
    public void AddGlobalListener(EventHandler<SignalEventArgs> listener)
    {
        if (listener is null)
            throw new ArgumentNullException(nameof(listener));
            
        if (!_globalListeners.Contains(listener))
        {
            _globalListeners.Add(listener);
        }
    }
    
    /// <summary>
    /// 移除全局监听器
    /// </summary>
    public void RemoveGlobalListener(EventHandler<SignalEventArgs> listener)
    {
        _globalListeners.Remove(listener);
    }
    
    /// <summary>
    /// 注册全局信号监听器实例
    /// </summary>
    public void RegisterGlobalListener(IGlobalSignalListener listener)
    {
        if (listener is not null && !_registeredGlobalListeners.Contains(listener))
        {
            _registeredGlobalListeners.Add(listener);
        }
    }
    
    /// <summary>
    /// 添加特定信号监听器
    /// </summary>
    public void AddSignalListener(string signalName, EventHandler<SignalEventArgs> listener)
    {
        if (string.IsNullOrEmpty(signalName) || listener is null)
            throw new ArgumentNullException($"{nameof(signalName)} or {nameof(listener)}");
        
        if (!_signalListeners.TryGetValue(signalName, out var listeners))
        {
            listeners = new List<Delegate>();
            _signalListeners[signalName] = listeners;
        }
        
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }
    
    /// <summary>
    /// 移除特定信号监听器
    /// </summary>
    public void RemoveSignalListener(string signalName, EventHandler<SignalEventArgs> listener)
    {
        if (_signalListeners.TryGetValue(signalName, out var listeners))
        {
            listeners.Remove(listener);
        }
    }
    
    /// <summary>
    /// 触发 SignalEmitted 事件
    /// </summary>
    private void OnSignalEmitted(SignalEventArgs e)
    {
        SignalEmitted?.Invoke(this, e);
    }
    
    /// <summary>
    /// 调用已注册的全局监听器（异步）
    /// </summary>
    private async Task InvokeRegisteredGlobalListenersAsync(object? sender, SignalEventArgs e)
    {
        foreach (var listener in _registeredGlobalListeners)
        {
            try
            {
                await listener.OnSignalEmittedAsync(sender, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"已注册的全局监听器异常：{ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 调用信号监听器
    /// </summary>
    private void InvokeSignalListeners(string signalName, SignalEventArgs e)
    {
        // 调用全局监听器
        foreach (var listener in _globalListeners)
        {
            try
            {
                ((EventHandler<SignalEventArgs>)listener).Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"全局监听器处理异常：{ex.Message}");
            }
        }
        
        // 调用特定信号监听器
        if (_signalListeners.TryGetValue(signalName, out var listeners))
        {
            foreach (var listener in listeners)
            {
                try
                {
                    ((EventHandler<SignalEventArgs>)listener).Invoke(this, e);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"信号监听器处理异常 [{signalName}]: {ex.Message}");
                }
            }
        }
    }
}
