# NotMediator 信号监听功能测试说明

## 测试项目结构

### 1. 单元测试类 (Test1.cs)
包含完整的 MSTest 单元测试，共 17 个测试方法：

- `Test_GlobalListener_CanReceiveAllSignals` - 测试全局监听器接收所有信号
- `Test_SignalListener_OnlyReceiveSpecificSignal` - 测试特定信号监听器只接收指定信号
- `Test_SignalEventArgs_ContainsCorrectData` - 测试信号事件数据完整性
- `Test_GetDataAs_ConvertDataSuccessfully` - 测试类型转换方法
- `Test_RemoveGlobalListener_StopReceivingEvents` - 测试移除监听器功能
- `Test_RemoveSignalListener_StopReceivingSpecificSignal` - 测试移除特定信号监听器
- `Test_OnSignalEmitted_EventSubscription_Works` - 测试事件订阅功能
- `Test_OffSignalEmitted_CancelEventSubscription` - 测试取消事件订阅
- `Test_MultipleListeners_AllReceiveEvents` - 测试多个监听器同时工作
- `Test_SignalTimestamp_IsRecentTime` - 测试时间戳准确性
- `Test_ListenerException_DoesNotAffectOtherListeners` - 测试异常隔离
- `Test_HandlerAndListener_BothReceiveSignal` - 测试处理器和监听器都能接收
- `Test_EmitSignal_WithDifferentParameterCounts` - 测试不同参数数量的信号
- `Test_ListenerCanAccessSenderInfo` - 测试发送者信息访问
- `Test_AddNullListener_ThrowsException` - 测试空值检查
- `Test_GlobalListener_ReceivesEventBeforeHandler` - 测试执行顺序

### 2. 手动测试程序 (ManualSignalTest.cs)
提供控制台版本的手动测试，可以直观看到测试过程。

## 运行测试

### 方法 1: 使用 dotnet test 命令
```bash
cd F:\gitprogrem\NotMediator\NotMediator-Test
dotnet test --verbosity normal
```

查看详细信息：
```bash
dotnet test --logger "console;verbosity=detailed"
```

运行特定测试：
```bash
dotnet test --filter "FullyQualifiedName~SignalListenerTests"
```

### 方法 2: 使用 Visual Studio
1. 打开 NotMediator.sln 解决方案
2. 右键点击解决方案 -> 生成
3. 测试 -> 资源管理器
4. 在测试资源管理器中运行 SignalListenerTests

### 方法 3: 运行手动测试程序
修改 Program.cs 或创建新的入口：

```csharp
using NotMediator_Test;

class Program
{
    static void Main(string[] args)
    {
        ManualSignalTest.RunAllTests();
    }
}
```

然后运行：
```bash
dotnet run
```

## 测试覆盖的功能点

### ✅ 核心功能
- 全局监听器（AddGlobalListener）
- 特定信号监听器（AddSignalListener）
- 移除监听器（RemoveGlobalListener, RemoveSignalListener）
- 事件订阅（OnSignalEmitted, OffSignalEmitted）

### ✅ 数据验证
- 信号名称
- 信号数据（数量、内容）
- 发送者信息
- 时间戳

### ✅ 数据操作
- GetData(int index) - 按索引获取数据
- GetDataAs<T>() - 类型安全的转换

### ✅ 健壮性测试
- 空值检查（ArgumentNullException）
- 异常隔离（一个监听器异常不影响其他）
- 多个监听器并发工作
- 执行顺序验证

### ✅ 边界条件
- 无参数信号
- 单参数信号
- 多参数信号
- 监听器的添加和移除

## SignalEventArgs API

```csharp
public class SignalEventArgs : EventArgs
{
    // 基本信息
    string SignalName { get; }           // 信号名称
    ReadOnlyCollection<object?> SignalData { get; }  // 信号数据
    object? Sender { get; }              // 发送者
    DateTime Timestamp { get; }          // 时间戳
    
    // 辅助方法
    int DataCount { get; }               // 数据数量
    object? GetData(int index);          // 获取指定索引的数据
    T? GetDataAs<T>();                   // 转换为指定类型
}
```

## 预期输出示例

```
=== 开始信号监听功能测试 ===

[测试] 全局监听器接收所有信号
  ✓ 监听到信号：Signal1
  ✓ 监听到信号：Signal2
  ✓ 监听到信号：Signal3
  ✓ 通过 - 收到 3 个信号

[测试] 特定信号监听器只接收指定信号
  ✓ 监听到目标信号：TargetSignal
  ✓ 通过 - 只收到目标信号

[测试] 信号事件数据包含正确信息
  ✓ 通过 - 信号名称：TestSignal, 数据数量：3
     数据 1: string_data
     数据 2: 42
     数据 3: True

... (更多测试输出)

=== 所有手动测试完成 ===
```

## 故障排除

### 问题 1: 编译错误
确保已正确引用 NotMediator 项目：
```xml
<ItemGroup>
  <ProjectReference Include="..\NotMediator\NotMediator.csproj" />
</ItemGroup>
```

### 问题 2: 测试未找到
检查命名空间是否正确：
```csharp
namespace NotMediator_Test;  // 必须与项目默认命名空间一致
```

### 问题 3: 测试失败
查看详细错误信息，常见原因：
- 监听器未被调用 -> 检查 EmitSignal 是否正确传递 sender 参数
- 数据不匹配 -> 检查参数数量和类型
- 异常未捕获 -> 检查 InvokeSignalListeners 中的异常处理

## 代码示例

### 基本使用
```csharp
var signalObject = new MySignalObject();

// 添加全局监听器
signalObject.AddGlobalListener((sender, e) => 
{
    Console.WriteLine($"信号：{e.SignalName}");
    Console.WriteLine($"数据：{e.DataCount} 个");
});

// 添加特定信号监听器
signalObject.AddSignalListener("MySignal", (sender, e) => 
{
    var data = e.GetDataAs<string>();
    Console.WriteLine($"收到：{data}");
});

// 触发信号
signalObject.EmitSignal("MySignal", "Hello World");
```

### 移除监听器
```csharp
EventHandler<SignalEventArgs> myListener = (sender, e) => 
{
    // 处理逻辑
};

signalObject.AddGlobalListener(myListener);
// ... 使用一段时间后
signalObject.RemoveGlobalListener(myListener);
```

### 类型安全的数据访问
```csharp
signalObject.AddGlobalListener((sender, e) => 
{
    if (e.DataCount > 0)
    {
        var name = e.GetDataAs<string>();
        var age = e.GetDataAs<int>();
        var isActive = e.GetDataAs<bool>();
    }
});
```
