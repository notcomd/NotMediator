using Microsoft.Extensions.DependencyInjection;
using NotMediator;

namespace NotMediator_Test;

[TestClass]
public sealed class GlobalSignalListenerTests
{
    private class TestSignalObject : NotSignalObject
    {
        public void EmitTestSignal(string data)
        {
            EmitSignal("TestSignal", data);
        }
    }
    
    private class TestGlobalListener : IGlobalSignalListener
    {
        public bool Called { get; set; }
        public SignalEventArgs? LastEventArgs { get; set; }
        
        public Task OnSignalEmittedAsync(object? sender, SignalEventArgs e)
        {
            Called = true;
            LastEventArgs = e;
            return Task.CompletedTask;
        }
    }
    
    [TestMethod]
    public void Test_GlobalListener_AutoRegisteredAndCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var testListener = new TestGlobalListener();
        services.AddSingleton<IGlobalSignalListener>(testListener);
        var serviceProvider = services.BuildServiceProvider();
        
        SignalObjectFactory.Initialize(serviceProvider);
        
        var signalObject = SignalObjectFactory.Create<TestSignalObject>();
        
        // Act
        signalObject.EmitTestSignal("TestData");
        
        // Allow time for async processing
        Task.Delay(100).Wait();
        
        // Assert
        Assert.IsTrue(testListener.Called);
        Assert.IsNotNull(testListener.LastEventArgs);
        Assert.AreEqual("TestSignal", testListener.LastEventArgs.SignalName);
        Assert.AreEqual("TestData", testListener.LastEventArgs.GetDataAs<string>());
    }
    
    [TestMethod]
    public void Test_MultipleGlobalListeners_AllCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var listener1 = new TestGlobalListener();
        var listener2 = new TestGlobalListener();
        services.AddSingleton<IGlobalSignalListener>(listener1);
        services.AddSingleton<IGlobalSignalListener>(listener2);
        var serviceProvider = services.BuildServiceProvider();
        
        SignalObjectFactory.Initialize(serviceProvider);
        
        var signalObject = SignalObjectFactory.Create<TestSignalObject>();
        
        // Act
        signalObject.EmitTestSignal("MultiTest");
        Task.Delay(100).Wait();
        
        // Assert
        Assert.IsTrue(listener1.Called);
        Assert.IsTrue(listener2.Called);
    }
    
    [TestMethod]
    public void Test_SignalObjectWithoutFactory_NoGlobalListeners()
    {
        // Arrange
        var services = new ServiceCollection();
        var testListener = new TestGlobalListener();
        services.AddSingleton<IGlobalSignalListener>(testListener);
        var serviceProvider = services.BuildServiceProvider();
        
        // 不使用 SignalObjectFactory，直接创建实例
        var signalObject = new TestSignalObject();
        
        // Act
        signalObject.EmitTestSignal("NoFactory");
        Task.Delay(100).Wait();
        
        // Assert - 因为没有初始化全局监听器，所以不会被调用
        Assert.IsFalse(testListener.Called);
    }
    
    [TestMethod]
    public void Test_GlobalLoggingListener_Integration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IGlobalSignalListener>(new GlobalSignalLoggingListener("[测试日志]"));
        var serviceProvider = services.BuildServiceProvider();
        
        SignalObjectFactory.Initialize(serviceProvider);
        var signalObject = SignalObjectFactory.Create<TestSignalObject>();
        
        // Act & Assert - 不抛出异常即表示成功
        try
        {
            signalObject.EmitTestSignal("LogTest");
            Task.Delay(100).Wait();
            Assert.IsTrue(true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"不应抛出异常：{ex.Message}");
        }
    }
    
    [TestMethod]
    public void Test_GlobalAuditListener_RecordCreated()
    {
        // Arrange
        var services = new ServiceCollection();
        var auditListener = new GlobalSignalAuditListener(storeInMemory: true);
        services.AddSingleton<IGlobalSignalListener>(auditListener);
        var serviceProvider = services.BuildServiceProvider();
        
        SignalObjectFactory.Initialize(serviceProvider);
        var signalObject = SignalObjectFactory.Create<TestSignalObject>();
        
        // Act
        signalObject.EmitTestSignal("AuditTest");
        Task.Delay(100).Wait();
        
        // Assert
        var records = auditListener.GetAuditRecords();
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("TestSignal", records[0].SignalName);
        Assert.AreEqual(1, records[0].DataCount);
    }
}
