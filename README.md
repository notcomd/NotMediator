# 一个通过反射实现了简易MediatoR的中介者消息传递类库

这里是使用示例
```csharp
///要发布的请求类
public class TestParent:IRequest<string>{
    public string Name { get; set; }
    public TestParent(string name)
    {
        Name = name;
    }
}
///实现请求的具体实现
public class TestParendHandler:IRequestHandler<TestParent,string>
{
    public Task<string> Handler(TestParent request, CancellationToken cancellationToken){
       return Task.FromResult($"{request.Name}");
    }
}
///管道依赖于请求的实现
public class LoggerPipeline:IPipelineBehavior<TestParent,string>{
    public Task<TResponse> Handle(TRequest request,Func<Task<TRequest>> next ,CancellationToken cancellationToken)
    {
      Console.WriteLine($"{nameof(LoggerPipeline)}");
      var result = await next();
      Console.WriteLine($"{result}");
      return result;
    }
}

```
这里是通知使用示例
```csharp
public class TestNotify:INotification{
    public string Name { get; set; }
    public TestNotify(string name){
    Name = name;
    }
}

public class TestNotifyHandler:INotificationHandler<TestNotify>{

    public Task Handle(TestNotify notification, CancellationToken cancellationToken){
    
    Console.WriteLine($"{notification.Name}");
    return Task.CompletedTask;
     }
}
```
对上面的使用示例进行注册
```csharp
///在Asp.net core中program.cs中添加
    builder.Services.AddMediator(Assembly.GetExecutingAssembly());
    //注册管道
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggerPipeline<,>));


```

如需在控制台应用中使用，请使用以下代码
需要引入 Microsoft.Extensions.DependencyInjection，NotMediator
```csharp
    
    var services=new ServiceProvider();
    services.AddMediator(Assembly.GetExecutingAssembly());
    //管道需要手动注册
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggerPipeline<,>));
    //获取中介者
    var mediator = services.GetService<IMediator>();
    //发送通知
    await mediator.Publish(new TestNotify("hello"));
    //发送请求
    await mediator.Send(new TestParent("hello"));
    Console.ReadLine();
```