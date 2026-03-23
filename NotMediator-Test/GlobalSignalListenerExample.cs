using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NotMediator;

namespace NotMediator_Test;

/// <summary>
/// 游戏系统示例 - 展示如何使用信号机制
/// </summary>
public class GameSystem : NotSignalObject
{
    public void Initialize()
    {
        // 添加特定信号监听器
        AddSignalListener("PlayerJoined", OnPlayerJoined);
        AddSignalListener("GameStarted", OnGameStarted);
        
        // 绑定信号处理器
        Connect("GameEnded", OnGameEnded);
    }
    
    private void OnPlayerJoined(object? sender, SignalEventArgs e)
    {
        var playerName = e.GetDataAs<string>();
        Console.WriteLine($">>> 玩家 {playerName} 加入了游戏！");
    }
    
    private void OnGameStarted(object? sender, SignalEventArgs e)
    {
        Console.WriteLine(">>> 游戏开始了！");
    }
    
    private void OnGameEnded()
    {
        Console.WriteLine(">>> 游戏结束了！");
    }
    
    public void PlayerJoin(string playerName)
    {
        EmitSignal("PlayerJoined", playerName);
    }
    
    public void StartGame()
    {
        EmitSignal("GameStarted");
    }
    
    public void EndGame()
    {
        EmitSignal("GameEnded");
    }
}

/// <summary>
/// 自定义全局信号监听器示例
/// </summary>
public class CustomGlobalSignalListener : IGlobalSignalListener
{
    public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
    {
        Console.WriteLine($"[自定义全局监听] 检测到信号：{e.SignalName}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 使用示例程序
/// </summary>
public static class GlobalSignalListenerExample
{
    public static async Task RunExample()
    {
        // 1. 配置依赖注入
        var services = new ServiceCollection();
        
        // 注册 NotMediator 和所有全局信号监听器
        services.AddNotMediatorWithGlobalSignalListeners(typeof(Program).Assembly);
        
        // 手动注册额外的全局监听器
        services.AddSingleton<IGlobalSignalListener>(new CustomGlobalSignalListener());
        services.AddSingleton<IGlobalSignalListener>(new GlobalSignalLoggingListener("[日志]"));
        services.AddSingleton<IGlobalSignalListener>(new GlobalSignalAuditListener(true));
        
        var serviceProvider = services.BuildServiceProvider();
        
        // 2. 初始化信号对象工厂
        SignalObjectFactory.Initialize(serviceProvider);
        
        // 3. 创建游戏系统实例（自动应用所有全局监听器）
        var gameSystem = SignalObjectFactory.Create<GameSystem>();
        gameSystem.Initialize();
        
        Console.WriteLine("=== 开始测试信号系统 ===\n");
        
        // 4. 触发信号 - 所有全局监听器会自动被调用
        gameSystem.PlayerJoin("张三");
        await Task.Delay(100);
        
        gameSystem.StartGame();
        await Task.Delay(100);
        
        gameSystem.EndGame();
        await Task.Delay(100);
        
        // 5. 查看审计记录
        var auditListener = serviceProvider.GetServices<IGlobalSignalListener>()
            .OfType<GlobalSignalAuditListener>()
            .FirstOrDefault();
            
        if (auditListener != null)
        {
            Console.WriteLine($"\n=== 审计记录 ({auditListener.GetAuditRecords().Count} 条) ===");
            foreach (var record in auditListener.GetAuditRecords())
            {
                Console.WriteLine($"[{record.Timestamp:HH:mm:ss}] {record.SignalName} - 发送者：{record.SenderType}");
            }
        }
        
        Console.WriteLine("\n=== 测试完成 ===");
    }
}
