using System.Reflection;
using System.Threading.Channels;

namespace NotMediator;

public class NotSignalManager
{
    private readonly Dictionary<string, List<Delegate>> _signalHandlers = new();
    private readonly List<Delegate> _globalListeners = new();
    private readonly Dictionary<string, List<Delegate>> _signalListeners = new();
    
    /// <summary>
    /// 信号触发事件
    /// </summary>
    public event EventHandler<SignalEventArgs>? SignalEmitted;
    
    /// <summary>
    /// 绑定信号
    /// </summary>
    /// <param name="signalName">
    /// 信号名称
    /// </param>
    /// <param name="handler">
    /// 信号处理函数
    /// </param>
    public void Connect(string signalName, Delegate handler)
    {
        if (string.IsNullOrEmpty(signalName) || handler is null)
        {
            throw new ArgumentNullException($"参数不能为空！{nameof(signalName)},and {nameof(handler)}");
        }

        if (!_signalHandlers.TryGetValue(signalName, out var handlers))
        {
            handlers = new List<Delegate>();
            _signalHandlers[signalName] = handlers;
        }

        if (!handlers.Contains(handler))
        {
            handlers.Add((handler));
        }
    }

    /// <summary>
    /// 解绑信号
    /// </summary>
    /// <param name="signalName">
    /// 信号名称
    /// </param>
    /// <param name="handler">
    /// 待解绑的信号处理函数
    /// </param>
    public void Disconnect(string signalName, Delegate handler)
    {
        if (string.IsNullOrEmpty(signalName) || handler is null)
        {
            return;
            return;
        }

        if (_signalHandlers.TryGetValue(signalName, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    /// <summary>
    /// 发送信号
    /// </summary>
    /// <param name="signalName">
    /// 信号名称 
    ///</param>
    /// <param name="signalData">
    /// 信号数据
    /// </param>
    public void Emit(string signalName, params object[] signalData)
    {
        Emit(signalName, null, signalData);
    }
    
    /// <summary>
    /// 发送信号
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="sender">信号发送者</param>
    /// <param name="signalData">信号数据</param>
    public void Emit(string signalName, object? sender, params object[] signalData)
    {
        var eventArgs = new SignalEventArgs(signalName, signalData, sender);
        
        OnSignalEmitted(eventArgs);
        
        InvokeSignalListeners(signalName, eventArgs);
        
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
    /// 添加全局信号监听器（监听所有信号）
    /// </summary>
    /// <param name="listener">监听器，接收 SignalEventArgs 参数</param>
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
    /// 移除全局信号监听器
    /// </summary>
    /// <param name="listener">监听器</param>
    public void RemoveGlobalListener(EventHandler<SignalEventArgs> listener)
    {
        _globalListeners.Remove(listener);
    }
    
    /// <summary>
    /// 添加特定信号的监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器，接收 SignalEventArgs 参数</param>
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
    /// 移除特定信号的监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器</param>
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
    /// <param name="e">信号事件参数</param>
    protected virtual void OnSignalEmitted(SignalEventArgs e)
    {
        SignalEmitted?.Invoke(this, e);
    }
    
    /// <summary>
    /// 调用信号监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="e">信号事件参数</param>
    private void InvokeSignalListeners(string signalName, SignalEventArgs e)
    {
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