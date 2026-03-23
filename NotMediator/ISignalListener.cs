namespace NotMediator;

/// <summary>
/// 全局信号监听器接口，用于实现自动注册的全局监听
/// </summary>
public interface IGlobalSignalListener
{
    /// <summary>
    /// 当任何信号被触发时调用
    /// </summary>
    /// <param name="sender">信号发送者</param>
    /// <param name="e">信号事件参数</param>
    Task OnSignalEmittedAsync(object? sender, SignalEventArgs e);
}

/// <summary>
/// 特定信号监听器接口，用于监听指定信号
/// </summary>
/// <typeparam name="TSignalListener">监听器类型（用于标记）</typeparam>
public interface ISignalListener<TSignalListener> : IGlobalSignalListener
{
    /// <summary>
    /// 监听的信号名称
    /// </summary>
    string SignalName { get; }
    
    /// <summary>
    /// 当指定信号被触发时调用
    /// </summary>
    /// <param name="sender">信号发送者</param>
    /// <param name="e">信号事件参数</param>
    new Task OnSignalEmittedAsync(object? sender, SignalEventArgs e);
}
