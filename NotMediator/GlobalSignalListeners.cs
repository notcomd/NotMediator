namespace NotMediator;

/// <summary>
/// 全局信号日志监听器 - 自动记录所有信号触发
/// </summary>
public class GlobalSignalLoggingListener : IGlobalSignalListener
{
    private readonly string _logPrefix;
    
    public GlobalSignalLoggingListener(string logPrefix = "[信号日志]")
    {
        _logPrefix = logPrefix;
    }
    
    public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
    {
        Console.WriteLine($"{_logPrefix} [{e.Timestamp:HH:mm:ss}] 信号 '{e.SignalName}' 被触发");
        Console.WriteLine($"{_logPrefix}   发送者：{sender?.GetType().Name ?? "null"}");
        
        if (e.DataCount > 0)
        {
            var dataStr = string.Join(", ", e.SignalData.Select(d => d?.ToString() ?? "null"));
            Console.WriteLine($"{_logPrefix}   数据 ({e.DataCount}): {dataStr}");
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// 全局信号审计监听器 - 记录信号的详细信息用于审计
/// </summary>
public class GlobalSignalAuditListener : IGlobalSignalListener
{
    private readonly List<SignalAuditRecord> _auditRecords = new();
    private readonly bool _storeInMemory;
    
    public GlobalSignalAuditListener(bool storeInMemory = true)
    {
        _storeInMemory = storeInMemory;
    }
    
    public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
    {
        var record = new SignalAuditRecord
        {
            Timestamp = e.Timestamp,
            SignalName = e.SignalName,
            SenderType = sender?.GetType().FullName,
            DataCount = e.DataCount,
            Data = e.SignalData.ToArray()
        };
        
        if (_storeInMemory)
        {
            _auditRecords.Add(record);
        }
        
        // 可以在这里写入数据库、文件等
        WriteToAuditLog(record);
        
        return Task.CompletedTask;
    }
    
    private void WriteToAuditLog(SignalAuditRecord record)
    {
        Console.WriteLine($"[审计] 记录信号：{record.SignalName} at {record.Timestamp}");
    }
    
    /// <summary>
    /// 获取所有审计记录（如果启用了内存存储）
    /// </summary>
    public IReadOnlyList<SignalAuditRecord> GetAuditRecords()
    {
        return _auditRecords.AsReadOnly();
    }
    
    /// <summary>
    /// 清除审计记录
    /// </summary>
    public void ClearAuditRecords()
    {
        _auditRecords.Clear();
    }
}

/// <summary>
/// 信号审计记录
/// </summary>
public class SignalAuditRecord
{
    public DateTime Timestamp { get; set; }
    public string SignalName { get; set; } = string.Empty;
    public string? SenderType { get; set; }
    public int DataCount { get; set; }
    public object?[] Data { get; set; } = Array.Empty<object?>();
}
