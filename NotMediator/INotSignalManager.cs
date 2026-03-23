namespace NotMediator;

/// <summary>
/// 信号管理器接口
/// </summary>
public interface INotSignalManager
{
    /// <summary>
    /// 信号触发事件
    /// </summary>
    event EventHandler<SignalEventArgs>? SignalEmitted;
    
    /// <summary>
    /// 绑定信号处理器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="handler">处理函数</param>
    void Connect(string signalName, Delegate handler);
    
    /// <summary>
    /// 解绑信号处理器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="handler">处理函数</param>
    void Disconnect(string signalName, Delegate handler);
    
    /// <summary>
    /// 发送信号（无发送者）
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="signalData">信号数据</param>
    void Emit(string signalName, params object[] signalData);
    
    /// <summary>
    /// 发送信号（带发送者）
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="sender">信号发送者</param>
    /// <param name="signalData">信号数据</param>
    void Emit(string signalName, object? sender, params object[] signalData);
    
    /// <summary>
    /// 添加全局监听器
    /// </summary>
    /// <param name="listener">监听器</param>
    void AddGlobalListener(EventHandler<SignalEventArgs> listener);
    
    /// <summary>
    /// 移除全局监听器
    /// </summary>
    /// <param name="listener">监听器</param>
    void RemoveGlobalListener(EventHandler<SignalEventArgs> listener);
    
    /// <summary>
    /// 注册全局信号监听器实例
    /// </summary>
    /// <param name="listener">全局监听器实例</param>
    void RegisterGlobalListener(IGlobalSignalListener listener);
    
    /// <summary>
    /// 添加特定信号监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器</param>
    void AddSignalListener(string signalName, EventHandler<SignalEventArgs> listener);
    
    /// <summary>
    /// 移除特定信号监听器
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="listener">监听器</param>
    void RemoveSignalListener(string signalName, EventHandler<SignalEventArgs> listener);
}
