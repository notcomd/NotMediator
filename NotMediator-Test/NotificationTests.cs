using Microsoft.Extensions.DependencyInjection;
using NotMediator;

namespace NotMediator_Test;

/// <summary>
/// INotifications 通知功能测试
/// </summary>
[TestClass]
public sealed class NotificationTests
{
    private class TestNotification : INotifications
    {
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    
    private class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public bool HandlerCalled { get; private set; }
        public TestNotification? ReceivedNotification { get; private set; }
        
        public Task Handler(TestNotification notifications, CancellationToken cancellationToken = default)
        {
            HandlerCalled = true;
            ReceivedNotification = notifications;
            return Task.CompletedTask;
        }
    }
    
    private class MultipleHandler1 : INotificationHandler<TestNotification>
    {
        public static bool Called { get; private set; }
        
        public Task Handler(TestNotification notifications, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.CompletedTask;
        }
    }
    
    private class MultipleHandler2 : INotificationHandler<TestNotification>
    {
        public static bool Called { get; private set; }
        
        public Task Handler(TestNotification notifications, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.CompletedTask;
        }
    }
    
    [TestMethod]
    public void Test_PublishAsync_CallsNotificationHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        
        var handler = new TestNotificationHandler();
        services.AddTransient<INotificationHandler<TestNotification>>(_ => handler);
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var notification = new TestNotification 
        { 
            Message = "Test Message", 
            Count = 42 
        };
        
        // Act
         mediator.PublishAsync(notification);
        
        // Allow time for async processing
         Task.Delay(100);
        
        // Assert
        Assert.IsTrue(handler.HandlerCalled);
        Assert.IsNotNull(handler.ReceivedNotification);
        Assert.AreEqual("Test Message", handler.ReceivedNotification.Message);
        Assert.AreEqual(42, handler.ReceivedNotification.Count);
    }
    
    [TestMethod]
    public void Test_MultipleHandlers_AllCalledOnPublish()
    {
        // Arrange
        var services = new ServiceCollection();
        
        services.AddTransient<MultipleHandler1>();
        services.AddTransient<MultipleHandler2>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var notification = new TestNotification();
        
        // Act
         mediator.PublishAsync(notification);
        
        // Allow time for async processing
         Task.Delay(100);
        
        // Assert
        Assert.IsTrue(MultipleHandler1.Called);
        Assert.IsTrue(MultipleHandler2.Called);
    }
    
    [TestMethod]
    public void Test_PublishAsync_WithNullNotification_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        // Act & Assert
         Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => 
            await mediator.PublishAsync<TestNotification>(null!)
        );
    }
    
    [TestMethod]
    public void Test_NotificationHandler_ReceivesCorrectDataType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        
        var handler = new TestNotificationHandler();
        services.AddTransient<INotificationHandler<TestNotification>>(_ => handler);
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var notification = new TestNotification 
        { 
            Message = "Type Test", 
            Count = 99 
        };
        
        // Act
         mediator.PublishAsync(notification);
         Task.Delay(100);
        
        // Assert
        Assert.IsNotNull(handler.ReceivedNotification);
        Assert.IsInstanceOfType(handler.ReceivedNotification, typeof(TestNotification));
        Assert.AreEqual("Type Test", handler.ReceivedNotification.Message);
        Assert.AreEqual(99, handler.ReceivedNotification.Count);
    }
    
    [TestMethod]
    public void Test_CompleteAsync_WaitsForAllNotifications()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        
        var completionCount = 0;
        
        var handler = new TestNotificationHandler();
        services.AddTransient<INotificationHandler<TestNotification>>(sp => 
        {
            completionCount++;
            return handler;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        // Act
         mediator.PublishAsync(new TestNotification());
         mediator.CompleteAsync();
        
        // Assert
        Assert.IsTrue(completionCount > 0);
    }
}
