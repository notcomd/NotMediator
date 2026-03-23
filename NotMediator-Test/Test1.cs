using NotMediator;

namespace NotMediator_Test;

[TestClass]
public sealed class SignalListenerTests
{
    /// <summary>
    /// 用于测试的信号对象
    /// </summary>
    private class TestSignalObject : NotSignalObject
    {
        public List<string> ExecutedHandlers { get; } = new();
        
        public void RegisterTestHandler(string signalName, Action handler)
        {
            Connect(signalName, handler);
        }
    }
    
    [TestMethod]
    public void Test_GlobalListener_CanReceiveAllSignals()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var receivedEvents = new List<SignalEventArgs>();
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            receivedEvents.Add(e);
        });
        
        // Act
        signalObject.EmitSignal("Signal1", "data1");
        signalObject.EmitSignal("Signal2", "data2", 123);
        signalObject.EmitSignal("Signal3");
        
        // Assert
        Assert.AreEqual(3, receivedEvents.Count);
        Assert.AreEqual("Signal1", receivedEvents[0].SignalName);
        Assert.AreEqual("Signal2", receivedEvents[1].SignalName);
        Assert.AreEqual("Signal3", receivedEvents[2].SignalName);
    }
    
    [TestMethod]
    public void Test_SignalListener_OnlyReceiveSpecificSignal()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var receivedSignal = string.Empty;
        
        signalObject.AddSignalListener("TargetSignal", (sender, e) => 
        {
            receivedSignal = e.SignalName;
        });
        
        // Act
        signalObject.EmitSignal("OtherSignal", "ignored");
        signalObject.EmitSignal("TargetSignal", "received");
        signalObject.EmitSignal("AnotherSignal", "ignored");
        
        // Assert
        Assert.AreEqual("TargetSignal", receivedSignal);
    }
    
    [TestMethod]
    public void Test_SignalEventArgs_ContainsCorrectData()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        SignalEventArgs? capturedEvent = null;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            capturedEvent = e;
        });
        
        // Act
        signalObject.EmitSignal("TestSignal", "string_data", 42, true);
        
        // Assert
        Assert.IsNotNull(capturedEvent);
        Assert.AreEqual("TestSignal", capturedEvent.SignalName);
        Assert.AreEqual(signalObject, capturedEvent.Sender);
        Assert.AreEqual(3, capturedEvent.DataCount);
        Assert.AreEqual("string_data", capturedEvent.GetData(0));
        Assert.AreEqual(42, capturedEvent.GetData(1));
        Assert.IsTrue(capturedEvent.GetData(2) as bool?);
    }
    
    [TestMethod]
    public void Test_GetDataAs_ConvertDataSuccessfully()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        string? convertedData = null;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            convertedData = e.GetDataAs<string>();
        });
        
        // Act
        signalObject.EmitSignal("TestSignal", "test_value");
        
        // Assert
        Assert.AreEqual("test_value", convertedData);
    }
    
    [TestMethod]
    public void Test_RemoveGlobalListener_StopReceivingEvents()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var receiveCount = 0;
        
        EventHandler<SignalEventArgs> listener = (sender, e) => 
        {
            receiveCount++;
        };
        
        signalObject.AddGlobalListener(listener);
        signalObject.EmitSignal("Signal1");
        
        // Act - Remove listener
        signalObject.RemoveGlobalListener(listener);
        signalObject.EmitSignal("Signal2");
        signalObject.EmitSignal("Signal3");
        
        // Assert
        Assert.AreEqual(1, receiveCount);
    }
    
    [TestMethod]
    public void Test_RemoveSignalListener_StopReceivingSpecificSignal()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var receiveCount = 0;
        
        EventHandler<SignalEventArgs> listener = (sender, e) => 
        {
            receiveCount++;
        };
        
        signalObject.AddSignalListener("MySignal", listener);
        signalObject.EmitSignal("MySignal");
        
        // Act - Remove listener
        signalObject.RemoveSignalListener("MySignal", listener);
        signalObject.EmitSignal("MySignal");
        signalObject.EmitSignal("MySignal");
        
        // Assert
        Assert.AreEqual(1, receiveCount);
    }
    
    [TestMethod]
    public void Test_OnSignalEmitted_EventSubscription_Works()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var eventReceived = false;
        
        signalObject.OnSignalEmitted((sender, e) => 
        {
            eventReceived = true;
        });
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.IsTrue(eventReceived);
    }
    
    [TestMethod]
    public void Test_OffSignalEmitted_CancelEventSubscription()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var receiveCount = 0;
        
        EventHandler<SignalEventArgs> handler = (sender, e) => 
        {
            receiveCount++;
        };
        
        signalObject.OnSignalEmitted(handler);
        signalObject.EmitSignal("Signal1");
        
        // Act - Unsubscribe
        signalObject.OffSignalEmitted(handler);
        signalObject.EmitSignal("Signal2");
        
        // Assert
        Assert.AreEqual(1, receiveCount);
    }
    
    [TestMethod]
    public void Test_MultipleListeners_AllReceiveEvents()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var listener1Called = false;
        var listener2Called = false;
        var listener3Called = false;
        
        signalObject.AddGlobalListener((sender, e) => listener1Called = true);
        signalObject.AddGlobalListener((sender, e) => listener2Called = true);
        signalObject.AddSignalListener("TestSignal", (sender, e) => listener3Called = true);
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.IsTrue(listener1Called);
        Assert.IsTrue(listener2Called);
        Assert.IsTrue(listener3Called);
    }
    
    [TestMethod]
    public void Test_SignalTimestamp_IsRecentTime()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        DateTime capturedTimestamp = DateTime.MinValue;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            capturedTimestamp = e.Timestamp;
        });
        
        var beforeEmit = DateTime.Now;
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        var afterEmit = DateTime.Now;
        
        // Assert
        Assert.IsTrue(capturedTimestamp >= beforeEmit);
        Assert.IsTrue(capturedTimestamp <= afterEmit);
    }
    
    [TestMethod]
    public void Test_ListenerException_DoesNotAffectOtherListeners()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var secondListenerCalled = false;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            throw new Exception("Test exception");
        });
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            secondListenerCalled = true;
        });
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.IsTrue(secondListenerCalled);
    }
    
    [TestMethod]
    public void Test_HandlerAndListener_BothReceiveSignal()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var handlerCalled = false;
        var listenerCalled = false;
        
        signalObject.RegisterTestHandler("TestSignal", () => handlerCalled = true);
        signalObject.AddGlobalListener((sender, e) => listenerCalled = true);
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.IsTrue(handlerCalled);
        Assert.IsTrue(listenerCalled);
    }
    
    [TestMethod]
    public void Test_EmitSignal_WithDifferentParameterCounts()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var testData = new List<int>();
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            testData.Add(e.DataCount);
        });
        
        // Act
        signalObject.EmitSignal("NoParams");
        signalObject.EmitSignal("OneParam", "data1");
        signalObject.EmitSignal("TwoParams", "data1", "data2");
        signalObject.EmitSignal("ThreeParams", "data1", "data2", "data3");
        
        // Assert
        Assert.AreEqual(0, testData[0]);
        Assert.AreEqual(1, testData[1]);
        Assert.AreEqual(2, testData[2]);
        Assert.AreEqual(3, testData[3]);
    }
    
    [TestMethod]
    public void Test_ListenerCanAccessSenderInfo()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        object? capturedSender = null;
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            capturedSender = e.Sender;
        });
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.AreEqual(signalObject, capturedSender);
    }
    
    [TestMethod]
    public void Test_AddNullListener_ThrowsException()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            signalObject.AddGlobalListener(null!)
        );
        
        Assert.ThrowsException<ArgumentNullException>(() => 
            signalObject.AddSignalListener("TestSignal", null!)
        );
    }
    
    [TestMethod]
    public void Test_GlobalListener_ReceivesEventBeforeHandler()
    {
        // Arrange
        var signalObject = new TestSignalObject();
        var executionOrder = new List<string>();
        
        signalObject.AddGlobalListener((sender, e) => 
        {
            executionOrder.Add("GlobalListener");
        });
        
        signalObject.RegisterTestHandler("TestSignal", () => 
        {
            executionOrder.Add("Handler");
        });
        
        // Act
        signalObject.EmitSignal("TestSignal");
        
        // Assert
        Assert.AreEqual(2, executionOrder.Count);
        Assert.AreEqual("GlobalListener", executionOrder[0]);
        Assert.AreEqual("Handler", executionOrder[1]);
    }
}