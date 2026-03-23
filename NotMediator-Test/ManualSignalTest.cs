using NotMediator;

namespace NotMediator_Test;

/// <summary>
/// 信号监听功能手动测试程序
/// </summary>
public class ManualSignalTest
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== 开始信号监听功能测试 ===\n");
        
        Test_GlobalListener_CanReceiveAllSignals();
        Test_SignalListener_OnlyReceiveSpecificSignal();
        Test_SignalEventArgs_ContainsCorrectData();
        Test_GetDataAs_ConvertDataSuccessfully();
        Test_RemoveGlobalListener_StopReceivingEvents();
        Test_MultipleListeners_AllReceiveEvents();
        Test_ListenerException_DoesNotAffectOtherListeners();
        
        Console.WriteLine("\n=== 所有手动测试完成 ===");
    }
    
    private static void Test_GlobalListener_CanReceiveAllSignals()
    {
        Console.WriteLine("[测试] 全局监听器接收所有信号");
        
        var signalObject = new TestSignalObject();
        var receivedEvents = new List<SignalEventArgs>();
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            receivedEvents.Add(e);
            Console.WriteLine($"  ✓ 监听到信号：{e.SignalName}");
        });
        
        signalObject.EmitSignal("Signal1", "data1");
        signalObject.EmitSignal("Signal2", "data2", 123);
        signalObject.EmitSignal("Signal3");
        
        if (receivedEvents.Count == 3)
        {
            Console.WriteLine($"  ✓ 通过 - 收到 {receivedEvents.Count} 个信号\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败 - 期望 3 个，实际 {receivedEvents.Count} 个\n");
        }
    }
    
    private static void Test_SignalListener_OnlyReceiveSpecificSignal()
    {
        Console.WriteLine("[测试] 特定信号监听器只接收指定信号");
        
        var signalObject = new TestSignalObject();
        var receivedSignals = new List<string>();
        
        signalObject.AddSignalListener("TargetSignal", (sender, e) => 
        {
            receivedSignals.Add(e.SignalName);
            Console.WriteLine($"  ✓ 监听到目标信号：{e.SignalName}");
        });
        
        signalObject.EmitSignal("OtherSignal", "ignored");
        signalObject.EmitSignal("TargetSignal", "received");
        signalObject.EmitSignal("AnotherSignal", "ignored");
        
        if (receivedSignals.Count == 1 && receivedSignals[0] == "TargetSignal")
        {
            Console.WriteLine($"  ✓ 通过 - 只收到目标信号\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败 - 收到 {receivedSignals.Count} 个信号\n");
        }
    }
    
    private static void Test_SignalEventArgs_ContainsCorrectData()
    {
        Console.WriteLine("[测试] 信号事件数据包含正确信息");
        
        var signalObject = new TestSignalObject();
        SignalEventArgs? capturedEvent = null;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            capturedEvent = e;
        });
        
        signalObject.EmitSignal("TestSignal", "string_data", 42, true);
        
        if (capturedEvent != null && 
            capturedEvent.SignalName == "TestSignal" && 
            capturedEvent.DataCount == 3)
        {
            Console.WriteLine($"  ✓ 通过 - 信号名称：{capturedEvent.SignalName}, 数据数量：{capturedEvent.DataCount}");
            Console.WriteLine($"     数据 1: {capturedEvent.GetData(0)}");
            Console.WriteLine($"     数据 2: {capturedEvent.GetData(1)}");
            Console.WriteLine($"     数据 3: {capturedEvent.GetData(2)}\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败\n");
        }
    }
    
    private static void Test_GetDataAs_ConvertDataSuccessfully()
    {
        Console.WriteLine("[测试] 类型转换方法 GetDataAs");
        
        var signalObject = new TestSignalObject();
        string? convertedData = null;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            convertedData = e.GetDataAs<string>();
        });
        
        signalObject.EmitSignal("TestSignal", "test_value");
        
        if (convertedData == "test_value")
        {
            Console.WriteLine($"  ✓ 通过 - 转换成功：{convertedData}\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败 - 转换结果：{convertedData}\n");
        }
    }
    
    private static void Test_RemoveGlobalListener_StopReceivingEvents()
    {
        Console.WriteLine("[测试] 移除全局监听器后停止接收");
        
        var signalObject = new TestSignalObject();
        var receiveCount = 0;
        
        EventHandler<SignalEventArgs> listener = (sender, e) => 
        {
            receiveCount++;
        };
        
        signalObject.AddGlobalListener(listener);
        signalObject.EmitSignal("Signal1");
        
        signalObject.RemoveGlobalListener(listener);
        signalObject.EmitSignal("Signal2");
        signalObject.EmitSignal("Signal3");
        
        if (receiveCount == 1)
        {
            Console.WriteLine($"  ✓ 通过 - 收到 {receiveCount} 次（移除前 1 次，移除后 0 次）\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败 - 收到 {receiveCount} 次\n");
        }
    }
    
    private static void Test_MultipleListeners_AllReceiveEvents()
    {
        Console.WriteLine("[测试] 多个监听器都能接收事件");
        
        var signalObject = new TestSignalObject();
        var listener1Called = false;
        var listener2Called = false;
        var listener3Called = false;
        
        signalObject.AddGlobalListener((sender, e) => 
        { 
            listener1Called = true;
            Console.WriteLine("  ✓ 监听器 1 被调用");
        });
        
        signalObject.AddGlobalListener((sender, e) => 
        { 
            listener2Called = true;
            Console.WriteLine("  ✓ 监听器 2 被调用");
        });
        
        signalObject.AddSignalListener("TestSignal", (sender, e) => 
        { 
            listener3Called = true;
            Console.WriteLine("  ✓ 监听器 3 被调用");
        });
        
        signalObject.EmitSignal("TestSignal");
        
        if (listener1Called && listener2Called && listener3Called)
        {
            Console.WriteLine($"  ✓ 通过 - 所有监听器都被调用\n");
        }
        else
        {
            Console.WriteLine($"  ✗ 失败\n");
        }
    }
    
    private static void Test_ListenerException_DoesNotAffectOtherListeners()
    {
        Console.WriteLine("[测试] 监听器异常不影响其他监听器");
        
        var signalObject = new TestSignalObject();
        var secondListenerCalled = false;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            Console.WriteLine("  ! 第一个监听器抛出异常");
            throw new Exception("Test exception");
        });
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            secondListenerCalled = true;
            Console.WriteLine("  ✓ 第二个监听器正常执行");
        });
        
        try
        {
            signalObject.EmitSignal("TestSignal");
            
            if (secondListenerCalled)
            {
                Console.WriteLine($"  ✓ 通过 - 第二个监听器未受影响\n");
            }
            else
            {
                Console.WriteLine($"  ✗ 失败 - 第二个监听器未被调用\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 失败 - 异常泄漏：{ex.Message}\n");
        }
    }
    
    /// <summary>
    /// 测试用的信号对象类
    /// </summary>
    private class TestSignalObject : NotSignalObject
    {
        public List<string> ExecutedHandlers { get; } = new();
        
        public void RegisterTestHandler(string signalName, Action handler)
        {
            Connect(signalName, handler);
        }
    }
}
