using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

public abstract class NotSignalObject
{
    
    protected readonly NotSignalManager NotSignalManager= new NotSignalManager();
    
    /// <summary>
    /// 标记是否已初始化全局监听器
    /// </summary>
    private bool _globalListenersInitialized = false;
    
    /// <summary>
    /// 初始化全局监听器（从服务提供者获取并注册）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    internal void InitializeGlobalListeners(IServiceProvider serviceProvider)
    {
        if (!_globalListenersInitialized)
        {
            var listeners = serviceProvider.GetServices<IGlobalSignalListener>();
            foreach (var listener in listeners)
            {
                NotSignalManager.RegisterGlobalListener(listener);
            }
            _globalListenersInitialized = true;
        }
    }
    
    
    public void Connect(string signalName, Delegate handler)
    {
        NotSignalManager.Connect(signalName, handler);
    }
    
    
    public void Disconnect(string signalName, Delegate handler)
    {
        NotSignalManager.Disconnect(signalName, handler);
    }
    
    public void EmitSignal(string signalName, params object[] signalData)
    {
        NotSignalManager.Emit(signalName, this, signalData);
    }
    
    /// <summary>
    /// 添加全局信号监听器，可以监听所有信号的触发
    /// </summary>
    /// <param name="listener">监听器回调，接收 SignalEventArgs 参数</param>
    public void AddGlobalListener(EventHandler<SignalEventArgs> listener)
    {
        NotSignalManager.AddGlobalListener(listener);
    }
    
    /// <summary>
    /// 移除全局信号监听器
    /// </summary>
    /// <param name="listener">监听器</param>
    public void RemoveGlobalListener(EventHandler<SignalEventArgs> listener)
    {
        NotSignalManager.RemoveGlobalListener(listener);
    }
    
    /// <summary>
    /// 添加特定信号的监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器回调，接收 SignalEventArgs 参数</param>
    public void AddSignalListener(string signalName, EventHandler<SignalEventArgs> listener)
    {
        NotSignalManager.AddSignalListener(signalName, listener);
    }
    
    /// <summary>
    /// 移除特定信号的监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器</param>
    public void RemoveSignalListener(string signalName, EventHandler<SignalEventArgs> listener)
    {
        NotSignalManager.RemoveSignalListener(signalName, listener);
    }
    
    /// <summary>
    /// 订阅信号触发事件
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void OnSignalEmitted(EventHandler<SignalEventArgs> handler)
    {
        NotSignalManager.SignalEmitted += handler;
    }
    
    /// <summary>
    /// 取消订阅信号触发事件
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void OffSignalEmitted(EventHandler<SignalEventArgs> handler)
    {
        NotSignalManager.SignalEmitted -= handler;
    }
    
}