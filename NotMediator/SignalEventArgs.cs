using System.Collections.ObjectModel;

namespace NotMediator;

public class SignalEventArgs:EventArgs
{
    /// <summary>
    /// 信号名称
    /// </summary>
    public string SignalName { get; }
    
    /// <summary>
    /// 信号数据
    /// </summary>
    public ReadOnlyCollection<object?> SignalData { get; }
    
    /// <summary>
    /// 信号发送者
    /// </summary>
    public object? Sender { get; }
    
    /// <summary>
    /// 信号触发的时间戳
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.Now;
    
    /// <summary>
    /// 获取信号数据的数量
    /// </summary>
    public int DataCount => SignalData?.Count ?? 0;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="signalData">信号数据</param>
    /// <param name="sender">信号发送者</param>
    public SignalEventArgs(string signalName, object?[] signalData, object? sender)
    {
        SignalName = signalName;
        SignalData = signalData.AsReadOnly();
        Sender = sender;
    }
    
    /// <summary>
    /// 获取指定索引的信号数据
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>信号数据</returns>
    public object? GetData(int index)
    {
        if (index < 0 || index >= DataCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return SignalData[index];
    }
    
    /// <summary>
    /// 尝试将第一个数据转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的数据，如果转换失败或没有数据则返回 default</returns>
    public T? GetDataAs<T>()
    {
        if (DataCount == 0)
            return default;
            
        try
        {
            return (T?)SignalData[0];
        }
        catch
        {
            return default;
        }
    }
}