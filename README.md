# NotMediator - 简易的中介者模式类库

一个基于反射实现的轻量级 Mediator 模式类库，支持请求/响应、通知发布/订阅以及管道行为。

## 核心特性

- ✅ 请求/响应模式（Request/Response）
- ✅ 通知发布/订阅模式（Publish/Subscribe）
- ✅ 管道行为支持（Pipeline Behavior）
- ✅ 信号机制（Signal System）
- ✅ 依赖注入集成
- ✅ 异步操作支持

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

```csharp
// 继承 NotSignalObject 基类来使用信号功能
public class GameSystem : NotSignalObject
{
    public void Initialize()
    {
        // 添加全局监听器 - 监听所有信号
        AddGlobalListener((sender, e) => 
        {
            Console.WriteLine($"[全局日志] 信号 '{e.SignalName}' 被触发");
            Console.WriteLine($"发送者：{e.Sender?.GetType().Name}");
            Console.WriteLine($"数据数量：{e.DataCount}");
        });
        
        // 添加特定信号监听器
        AddSignalListener("PlayerJoined", (sender, e) => 
        {
            var playerName = e.GetDataAs<string>();
            Console.WriteLine($">>> 玩家 {playerName} 加入了游戏！");
        });
        
        // 绑定信号处理器
        Connect("GameStarted", () => 
        {
            Console.WriteLine("游戏开始了！");
        });
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
}

// 使用示例
var gameSystem = new GameSystem();
gameSystem.Initialize();
gameSystem.PlayerJoin("张三");  // 输出：>>> 玩家 张三 加入了游戏！
gameSystem.StartGame();          // 输出：游戏开始了！
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

#### 高级信号功能

```csharp
public class AdvancedSignalExample : NotSignalObject
{
    public void Setup()
    {
        // 1. 全局监听器 - 监听所有信号
        AddGlobalListener(OnGlobalSignal);
        
        // 2. 特定信号监听器 - 只监听指定信号
        AddSignalListener("CriticalEvent", OnCriticalEvent);
        
        // 3. 订阅信号触发事件
        OnSignalEmitted(OnAnySignalEmitted);
        
        // 4. 移除监听器
        // RemoveGlobalListener(OnGlobalSignal);
        // RemoveSignalListener("CriticalEvent", OnCriticalEvent);
    }
    
    private void OnGlobalSignal(object? sender, SignalEventArgs e)
    {
        Console.WriteLine($"监听到信号：{e.SignalName}");
    }
    
    private void OnCriticalEvent(object? sender, SignalEventArgs e)
    {
        Console.WriteLine($"关键事件：{e.SignalName}");
    }
    
    private void OnAnySignalEmitted(object? sender, SignalEventArgs e)
    {
        // 记录所有信号的历史
        Console.WriteLine($"[{e.Timestamp}] {e.SignalName}");
    }
}
```

**信号机制特点：**
- 🎯 **灵活的事件系统** - 支持全局和特定信号监听
- 📊 **丰富的数据传递** - 支持任意数量和类型的参数
- 🔍 **完整的上下文信息** - 包含信号名称、发送者、时间戳等
- 🛡️ **异常隔离** - 单个监听器异常不影响其他监听器
- 🔄 **类型安全** - 提供 `GetDataAs<T>()` 进行类型转换

## 依赖注入配置

### ASP.NET Core 中使用

在 `Program.cs` 中注册服务：

```csharp
// 仅注册请求和通知处理器
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

// 如果需要自动注册管道行为
builder.Services.AddNotMediatorWithPipelineBehaviors(Assembly.GetExecutingAssembly());

// 或者手动注册特定管道
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

### 控制台应用中使用

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

var services = new ServiceCollection();

// 注册 NotMediator
services.AddNotMediator(Assembly.GetExecutingAssembly());

// 手动注册管道（如果需要）
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// 构建服务提供者
var serviceProvider = services.BuildServiceProvider();

// 获取中介者实例
var mediator = serviceProvider.GetService<INotMediator>();
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
