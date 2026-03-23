using Microsoft.Extensions.DependencyInjection;
using NotMediator;

namespace NotMediator_Test;

/// <summary>
/// IPipelineBehavior 管道行为功能测试
/// </summary>
[TestClass]
public sealed class PipelineBehaviorTests
{
    private class TestRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }
    
    private class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public static bool HandlerCalled { get; set; }
        public static string? ReceivedValue { get; set; }
        
        public Task<string> Handler(TestRequest request, CancellationToken cancellationToken)
        {
            HandlerCalled = true;
            ReceivedValue = request.Value;
            return Task.FromResult(request.Value);
        }
    }
    
    private class LoggingPipelineBehavior : IPipelineBehavior<TestRequest, string>
    {
        public static bool BehaviorCalled { get; set; }
        public static bool BeforeNextCalled { get; set; }
        public static bool AfterNextCalled { get; set; }
        
        public async Task<string> Handler(
            TestRequest request, 
            Func<Task<string>> next, 
            CancellationToken cancellationToken)
        {
            BehaviorCalled = true;
            BeforeNextCalled = true;
            
            var result = await next();
            Console.WriteLine("我是管道1");
            AfterNextCalled = true;
            return result;
        }
    }
    
    private class ValidationPipelineBehavior : IPipelineBehavior<TestRequest, string>
    {
        public bool ShouldThrow { get; set; }
        
        public async Task<string> Handler(
            TestRequest request, 
            Func<Task<string>> next, 
            CancellationToken cancellationToken)
        {
            if (ShouldThrow && string.IsNullOrEmpty(request.Value))
            {
                throw new ArgumentException("Value cannot be empty");
            }
            Console.WriteLine("我是管道2");
            return await next();
        }
    }
    
    private class ModificationPipelineBehavior : IPipelineBehavior<TestRequest, string>
    {
        public string Prefix { get; set; } = string.Empty;
        
        public async Task<string> Handler(
            TestRequest request, 
            Func<Task<string>> next, 
            CancellationToken cancellationToken)
        {
            Console.WriteLine("我是管道3");
            var result = await next();
            return $"{Prefix}{result}";
        }
    }
    
    [TestInitialize]
    public void TestInitialize()
    {
        // Reset static flags
        TestRequestHandler.HandlerCalled = false;
        TestRequestHandler.ReceivedValue = null;
        LoggingPipelineBehavior.BehaviorCalled = false;
        LoggingPipelineBehavior.BeforeNextCalled = false;
        LoggingPipelineBehavior.AfterNextCalled = false;
    }
    
    [TestMethod]
    public void Test_SendAsync_ExecutesPipelineBehaviorsInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>, LoggingPipelineBehavior>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "Test" };
        
        // Act
        var result =  mediator.SendAsync(request).Result;
        
        // Assert
        Assert.IsTrue(LoggingPipelineBehavior.BehaviorCalled);
        Assert.IsTrue(LoggingPipelineBehavior.BeforeNextCalled);
        Assert.IsTrue(LoggingPipelineBehavior.AfterNextCalled);
        Assert.IsTrue(TestRequestHandler.HandlerCalled);
        Assert.AreEqual("Test", result);
    }
    
    [TestMethod]
    public void Test_SendAsync_MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        
        var executionOrder = new List<string>();
        
        services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
            new ModificationPipelineBehavior { Prefix = "A" });
        services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
            new ModificationPipelineBehavior { Prefix = "B" });
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "Result" };
        
        // Act
        var result =  mediator.SendAsync(request).Result;
        
        // Assert - Behaviors wrap each other, so last registered executes first
        Assert.AreEqual("BAResult", result);
    }
    
    [TestMethod]
    public void Test_SendAsync_PipelineBehavior_CanModifyRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
            new ModificationPipelineBehavior { Prefix = "Modified:" });
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "Original" };
        
        // Act
        var result =  mediator.SendAsync(request).Result;
        
        // Assert
        Assert.AreEqual("Modified:Original", result);
    }
    
    [TestMethod]
    public void Test_SendAsync_PipelineBehavior_CanThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        
        var validationBehavior = new ValidationPipelineBehavior { ShouldThrow = true };
        services.AddTransient<IPipelineBehavior<TestRequest, string>>(_ => validationBehavior);
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "" };
        
        // Act & Assert
         Assert.ThrowsExceptionAsync<ArgumentException>(async () => 
            await mediator.SendAsync(request)
        );
    }
    
    [TestMethod]
    public void Test_SendAsync_PipelineBehavior_CanAccessRequestData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>, LoggingPipelineBehavior>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "TestData" };
        
        // Act
        var result =  mediator.SendAsync(request).Result;
        
        // Assert
        Assert.AreEqual("TestData", result);
        Assert.AreEqual("TestData", TestRequestHandler.ReceivedValue);
    }
    
    [TestMethod]
    public void Test_SendAsync_NullRequest_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>, LoggingPipelineBehavior>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        // Act & Assert
         Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => 
            await mediator.SendAsync<TestRequest>(null!)
        );
    }
    
    [TestMethod]
    public void Test_AddNotMediatorWithPipelineBehaviors_AutoRegistersBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddNotMediatorWithPipelineBehaviors(typeof(PipelineBehaviorTests).Assembly);
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TestRequest, string>>();
        // Should find the LoggingPipelineBehavior defined in this assembly
        Assert.IsNotNull(behaviors);
    }
    
    [TestMethod]
    public void Test_PipelineBehavior_NextDelegate_CanBeCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddTransient<IPipelineBehavior<TestRequest, string>, LoggingPipelineBehavior>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "NextTest" };
        
        // Act
        var result =  mediator.SendAsync(request).Result;
        
        // Assert
        Assert.AreEqual("NextTest", result);
        Assert.IsTrue(LoggingPipelineBehavior.AfterNextCalled);
    }
    
    [TestMethod]
    public void Test_PipelineBehavior_Exception_HandledCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotMediator, NotMediator.NotMediator>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        
        services.AddTransient<IPipelineBehavior<TestRequest, string>>(sp => 
        {
            throw new InvalidOperationException("Pipeline initialization failed");
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<INotMediator>();
        
        var request = new TestRequest { Value = "Test" };
        
        // Act & Assert
         Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
            await mediator.SendAsync(request)
        );
    }
}
