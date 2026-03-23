# NotMediator - 简易的中介者模式类库

一个基于反射实现的轻量级 Mediator 模式类库，支持请求/响应、通知发布/订阅以及管道行为。

## 核心特性

- ✅ 请求/响应模式（Request/Response）
- ✅ 通知发布/订阅模式（Publish/Subscribe）
- ✅ 管道行为支持（Pipeline Behavior）
- ✅ 信号机制（Signal System）- 支持全局自动注册监听器
- ✅ 依赖注入集成
- ✅ 异步操作支持
- ✅ 接口化设计 - 高可测试性和扩展性

## 安装

通过 NuGet 安装：
```bash
dotnet add package NotMediator
```

## 快速开始

### 1. 请求/响应模式

```csharp
// 定义请求类
public class TestRequest : IRequest<string>
{
    public string Name { get; set; }
    
    public TestRequest(string name)
    {
        Name = name;
    }
}

// 实现请求处理器
public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handler(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
```

### 2. 通知发布/订阅模式

```csharp
// 定义通知类
public class TestNotification : INotifications
{
    public string Message { get; set; }
    
    public TestNotification(string message)
    {
        Message = message;
    }
}

// 实现通知处理器
public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handler(TestNotification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"收到通知：{notification.Message}");
        return Task.CompletedTask;
    }
}
```

### 3. 管道行为（可选）

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handler(
        TRequest request, 
        Func<Task<TResponse>> next, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[管道] 开始处理请求：{typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[管道] 请求处理完成，结果：{response}");
        return response;
    }
}
```

### 4. 信号机制（Signal System）

#### 基础用法

```csharp
using NotMediator;

// 继承 NotSignalObject 基类来使用信号功能
public class GameSystem : NotSignalObject
{
    public void Initialize()
    {
        // 1. 添加全局监听器 - 监听所有信号
        AddGlobalListener((sender, e) => 
        {
            Console.WriteLine($"[全局日志] 信号 '{e.SignalName}' 被触发");
            Console.WriteLine($"发送者：{e.Sender?.GetType().Name ?? "null"}");
            Console.WriteLine($"数据数量：{e.DataCount}");
            Console.WriteLine($"时间戳：{e.Timestamp:HH:mm:ss}");
        });
        
        // 2. 添加特定信号监听器
        AddSignalListener("PlayerJoined", (sender, e) => 
        {
            var playerName = e.GetDataAs<string>();
            Console.WriteLine($">>> 玩家 {playerName} 加入了游戏！");
        });
        
        // 3. 绑定信号处理器（无参数委托）
        Connect("GameStarted", () => 
        {
            Console.WriteLine("游戏开始了！");
        });
        
        // 4. 订阅信号触发事件（事件方式）
        OnSignalEmitted += (sender, e) =>
        {
            Console.WriteLine($"[事件] 检测到信号：{e.SignalName}");
        };
    }
    
    public void StartGame()
    {
        // 触发信号（无参数）
        EmitSignal("GameStarted");
    }
    
    public void PlayerJoin(string playerName)
    {
        // 触发信号（带参数）
        EmitSignal("PlayerJoined", playerName);
    }
    
    public void Combat(string attacker, string defender, int damage)
    {
        // 触发信号（多个参数）
        EmitSignal("Combat", attacker, defender, damage);
    }
    
    public void RemoveListeners()
    {
        // 移除监听器
        RemoveGlobalListener(/* 监听器引用 */);
        RemoveSignalListener("PlayerJoined", /* 监听器引用 */);
    }
}

// 使用示例
var gameSystem = new GameSystem();
gameSystem.Initialize();

gameSystem.PlayerJoin("张三");  
// 输出:
// [全局日志] 信号 'PlayerJoined' 被触发
// 发送者：GameSystem
// 数据数量：1
// 时间戳：10:30:45
// >>> 玩家 张三 加入了游戏！
// [事件] 检测到信号：PlayerJoined

gameSystem.StartGame();          
// 输出:
// [全局日志] 信号 'GameStarted' 被触发
// 游戏开始了！
// [事件] 检测到信号：GameStarted
```

#### 高级用法：全局信号监听器（自动注册）

通过依赖注入自动注册全局监听器，所有通过工厂创建的信号对象都会自动应用这些监听器：

```csharp
using Microsoft.Extensions.DependencyInjection;
using NotMediator;

// 1. 定义自定义全局监听器
public class GlobalLoggingListener : IGlobalSignalListener
{
    private readonly string _prefix;
    
    public GlobalLoggingListener(string prefix = "[信号日志]")
    {
        _prefix = prefix;
    }
    
    public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
    {
        Console.WriteLine($"{_prefix} [{e.Timestamp:HH:mm:ss}] 信号：{e.SignalName}");
        Console.WriteLine($"{_prefix}   发送者：{sender?.GetType().Name ?? "null"}");
        
        if (e.DataCount > 0)
        {
            var dataStr = string.Join(", ", e.SignalData.Select(d => d?.ToString() ?? "null"));
            Console.WriteLine($"{_prefix}   数据 ({e.DataCount}): {dataStr}");
        }
        
        return Task.CompletedTask; // 异步处理，不阻塞主流程
    }
}

// 2. 配置依赖注入
var services = new ServiceCollection();

// 自动扫描并注册所有全局监听器
services.AddNotMediatorWithGlobalSignalListeners(typeof(Program).Assembly);

// 或者手动注册特定的全局监听器
services.AddSingleton<IGlobalSignalListener>(new GlobalLoggingListener("[日志]"));
services.AddSingleton<IGlobalSignalListener>(new GlobalSignalAuditListener(storeInMemory: true));

var serviceProvider = services.BuildServiceProvider();

// 3. 初始化信号对象工厂
SignalObjectFactory.Initialize(serviceProvider);

// 4. 创建信号对象（自动应用所有已注册的全局监听器）
var gameSystem = SignalObjectFactory.Create<GameSystem>();
gameSystem.Initialize();

// 现在触发信号时，全局监听器会自动被调用
gameSystem.PlayerJoin("李四");
// 输出示例:
// [日志] [10:30:45] 信号：PlayerJoined
// [日志]   发送者：GameSystem
// [日志]   数据 (1): 李四
// >>> 玩家 李四 加入了游戏！
```

**内置全局监听器：**
- `GlobalSignalLoggingListener` - 自动记录所有信号触发（可自定义前缀）
- `GlobalSignalAuditListener` - 审计追踪，可查询历史记录

```csharp
// 使用内置监听器
services.AddSingleton<IGlobalSignalListener>(
    new GlobalSignalLoggingListener("[我的日志]")
);

// 审计监听器（支持内存存储）
var auditListener = new GlobalSignalAuditListener(storeInMemory: true);
services.AddSingleton<IGlobalSignalListener>(auditListener);

// 后续可以获取审计记录
var records = auditListener.GetAuditRecords();
foreach (var record in records)
{
    Console.WriteLine($"[{record.Timestamp}] {record.SignalName}");
}
```

#### SignalEventArgs API

信号事件参数提供以下信息：

```csharp
public class SignalEventArgs : EventArgs
{
    // 基本信息
    string SignalName { get; }                    // 信号名称
    ReadOnlyCollection<object?> SignalData { get; }  // 信号数据
    object? Sender { get; }                       // 发送者
    DateTime Timestamp { get; }                   // 时间戳
    
    // 辅助方法
    int DataCount { get; }                        // 数据数量
    object? GetData(int index);                   // 获取指定索引的数据
    T? GetDataAs<T>();                            // 类型安全的转换
}
```

#### INotSignalManager 接口

信号管理器已接口化，支持 Mock 和扩展：

```csharp
public interface INotSignalManager
{
    event EventHandler<SignalEventArgs>? SignalEmitted;
    void Connect(string signalName, Delegate handler);
    void Disconnect(string signalName, Delegate handler);
    void Emit(string signalName, params object[] signalData);
    void Emit(string signalName, object? sender, params object[] signalData);
    void AddGlobalListener(EventHandler<SignalEventArgs> listener);
    void RemoveGlobalListener(EventHandler<SignalEventArgs> listener);
    void RegisterGlobalListener(IGlobalSignalListener listener);
    void AddSignalListener(string signalName, EventHandler<SignalEventArgs> listener);
    void RemoveSignalListener(string signalName, EventHandler<SignalEventArgs> listener);
}
```

#### 实战示例：完整的信号系统应用

```csharp
using NotMediator;
using Microsoft.Extensions.DependencyInjection;

// 场景 1: 简单的游戏事件系统
public class GameEventManager : NotSignalObject
{
    public void SetupListeners()
    {
        // 监听玩家升级信号
        AddSignalListener("PlayerLevelUp", (sender, e) =>
        {
            var playerId = e.GetData<int>(0);
            var newLevel = e.GetData<int>(1);
            Console.WriteLine($"玩家 {playerId} 升级到 {newLevel} 级！");
        });
        
        // 监听物品拾取信号
        AddSignalListener("ItemPickedUp", (sender, e) =>
        {
            var item = e.GetDataAs<Item>();
            Console.WriteLine($"拾取物品：{item?.Name}");
        });
    }
    
    public void PlayerLevelUp(int playerId, int newLevel)
    {
        EmitSignal("PlayerLevelUp", playerId, newLevel);
    }
    
    public void PickItem(Item item)
    {
        EmitSignal("ItemPickedUp", item);
    }
}

// 场景 2: 使用全局监听器进行日志记录和性能监控
public class PerformanceMonitorListener : IGlobalSignalListener
{
    private readonly Dictionary<string, List<long>> _signalTimings = new();
    
    public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
    {
        // 记录信号触发频率
        if (!_signalTimings.ContainsKey(e.SignalName))
        {
            _signalTimings[e.SignalName] = new List<long>();
        }
        _signalTimings[e.SignalName].Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        
        // 输出统计信息
        var count = _signalTimings[e.SignalName].Count;
        Console.WriteLine($"[性能监控] 信号 {e.SignalName} 已被触发 {count} 次");
        
        return Task.CompletedTask;
    }
    
    public void PrintStatistics()
    {
        foreach (var kvp in _signalTimings)
        {
            Console.WriteLine($"信号 '{kvp.Key}' 触发次数：{kvp.Value.Count}");
        }
    }
}

// 场景 3: 结合依赖注入的完整应用
public class Program
{
    public static async Task Main()
    {
        // 配置服务
        var services = new ServiceCollection();
        
        // 注册所有功能（包括全局监听器）
        services.AddNotMediatorWithGlobalSignalListeners(typeof(Program).Assembly);
        
        // 注册自定义监听器
        services.AddSingleton<IGlobalSignalListener>(new PerformanceMonitorListener());
        services.AddSingleton<IGlobalSignalListener>(new GlobalSignalLoggingListener());
        
        var serviceProvider = services.BuildServiceProvider();
        SignalObjectFactory.Initialize(serviceProvider);
        
        // 创建并使用
        var gameManager = SignalObjectFactory.Create<GameEventManager>();
        gameManager.SetupListeners();
        
        // 触发信号
        gameManager.PlayerLevelUp(1, 5);
        gameManager.PickItem(new Item { Name = "宝剑" });
    }
}

public class Item
{
    public string Name { get; set; } = string.Empty;
}
```

**信号机制特点：**
- 🎯 **灵活的事件系统** - 支持全局和特定信号监听
- 📊 **丰富的数据传递** - 支持任意数量和类型的参数
- 🔍 **完整的上下文信息** - 包含信号名称、发送者、时间戳等
- 🛡️ **异常隔离** - 单个监听器异常不影响其他监听器
- 🔄 **类型安全** - 提供 `GetDataAs<T>()` 进行类型转换
- ⚡ **异步处理** - 全局监听器异步调用，不阻塞主流程
- 🔧 **接口化设计** - `INotSignalManager` 接口支持 Mock 和单元测试
- 🚀 **自动注册** - 通过 DI 自动注册全局监听器到所有信号对象

## 依赖注入配置

### ASP.NET Core 中使用

在 `Program.cs` 中注册服务：

```csharp
// 仅注册请求和通知处理器
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

// 如果需要自动注册管道行为
builder.Services.AddNotMediatorWithPipelineBehaviors(Assembly.GetExecutingAssembly());

// 自动注册全局信号监听器（推荐）
builder.Services.AddNotMediatorWithGlobalSignalListeners(Assembly.GetExecutingAssembly());

// 或者手动注册特定管道
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// 手动注册全局监听器
builder.Services.AddSingleton<IGlobalSignalListener>(new GlobalSignalLoggingListener());
```

### 控制台应用中使用

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

var services = new ServiceCollection();

// 注册 NotMediator 和全局信号监听器
services.AddNotMediatorWithGlobalSignalListeners(Assembly.GetExecutingAssembly());

// 构建服务提供者
var serviceProvider = services.BuildServiceProvider();

// 初始化信号对象工厂
SignalObjectFactory.Initialize(serviceProvider);

// 获取中介者实例
var mediator = serviceProvider.GetService<INotMediator>();

// 创建信号对象（自动应用全局监听器）
var gameSystem = SignalObjectFactory.Create<GameSystem>();
```

## 使用示例

### 发送请求

```csharp
var request = new TestRequest("张三");
var result = await mediator.SendAsync(request);
Console.WriteLine(result); // 输出：Hello, 张三!
```

### 发布通知

```csharp
var notification = new TestNotification("你好，世界！");
await mediator.PublishAsync(notification);
// 所有订阅该通知的处理器都会被调用
```

### 完整示例

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using NotMediator;

// 配置依赖注入
var services = new ServiceCollection();
services.AddNotMediatorWithPipelineBehaviors(Assembly.GetExecutingAssembly());
var serviceProvider = services.BuildServiceProvider();

// 获取中介者
var mediator = serviceProvider.GetRequiredService<INotMediator>();

// 发送请求
var request = new TestRequest("李四");
var response = await mediator.SendAsync(request);
Console.WriteLine($"请求结果：{response}");

// 发布通知
var notification = new TestNotification("测试通知");
await mediator.PublishAsync(notification);

// 完成所有通知处理并释放资源
await mediator.CompleteAsync();
mediator.Dispose();
```

## 接口说明

### INotMediator

主接口，提供以下方法：

- `Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, ...)` - 发送请求
- `Task PublishAsync<TNotification>(TNotification notification, ...)` - 发布通知
- `Task CompleteAsync()` - 完成所有通知处理
- `void Dispose()` - 释放资源

### 核心接口

- `IRequest<TResponse>` - 请求接口
- `IRequestHandler<TRequest, TResponse>` - 请求处理器接口
- `INotifications` - 通知接口
- `INotificationHandler<TNotifications>` - 通知处理器接口
- `IPipelineBehavior<TRequest, TResponse>` - 管道行为接口
- `INotSignalManager` - 信号管理器接口（接口化设计，支持 Mock）
- `IGlobalSignalListener` - 全局信号监听器接口（自动注册）
- `SignalEventArgs` - 信号事件参数

## 高级用法

### 多个通知处理器

同一个通知可以有多个处理器，它们都会被调用：

```csharp
public class EmailNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handler(TestNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"发送邮件：{notification.Message}");
        return Task.CompletedTask;
    }
}

public class SmsNotificationHandler : INotificationHandler<TestNotification>
{
    public Task Handler(TestNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"发送短信：{notification.Message}");
        return Task.CompletedTask;
    }
}
```

### 自定义管道行为

管道行为可以用于：
- 日志记录
- 性能监控
- 异常处理
- 验证
- 事务管理

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handler(
        TRequest request, 
        Func<Task<TResponse>> next, 
        CancellationToken cancellationToken)
    {
        // 前置验证
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        
        // 执行下一个处理器或管道
        var response = await next();
        
        // 后置处理
        Console.WriteLine($"响应已生成：{response}");
        
        return response;
    }
}
```

## 注意事项

1. **生命周期管理**：`INotMediator` 实现了 `IDisposable`，使用完毕后请调用 `Dispose()` 或使用 `using` 语句
2. **通知处理**：通知是异步处理的，可以使用 `CompleteAsync()` 等待所有通知处理完成
3. **管道顺序**：管道行为按照注册顺序依次执行
4. **异常处理**：通知处理器中的异常不会影响其他处理器

## 许可证

本项目采用 GPL-3.0 许可证。
