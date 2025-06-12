using System.Reflection;

namespace NotMediator;

public class NotSignalManager
{
    
    private readonly Dictionary<string, List<Delegate>> _signalHandlers = new();

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
            _signalHandlers[signalName]=handlers;
            
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
            return;   return;
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
            }catch (Exception e)
            {
                Console.WriteLine($"发生错误：无法调用信号！{signalName}:{e.Message}");
                throw;
            }
        }
        
    }
    
}