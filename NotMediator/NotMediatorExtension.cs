using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

public static class NotMediatorExtension
{
    /// <summary>
    /// 注册 NotMediator 及其处理器。请注意管道服务需要手动添加。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">需要扫描的程序集，用于自动发现处理器并对其进行注册。</param>
    /// <returns>服务集合，支持链式调用。</returns>
    public static IServiceCollection AddNotMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<INotMediator, NotMediator>(sp => new NotMediator(sp));

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            foreach (var handlerInterface in type.GetInterfaces())
            {
                if (handlerInterface.IsGenericType)
                {
                    var def = handlerInterface.GetGenericTypeDefinition();
                    if (def == typeof(IRequestHandler<,>) || def == typeof(INotificationHandler<>))
                    {
                        services.AddTransient(handlerInterface, type);
                    }
                }
            }
        }

        return services;
    }

    public static IServiceCollection AddNotMediatorWithPipelineBehaviors(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddNotMediator(assemblies);

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            foreach (var behaviorInterface in type.GetInterfaces())
            {
                if (behaviorInterface.IsGenericType &&
                    behaviorInterface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                {
                    services.AddTransient(behaviorInterface, type);
                }
            }
        }

        return services;
    }
    
    /// <summary>
    /// 注册 NotMediator、处理器、管道行为以及全局信号监听器。
    /// 此方法会自动扫描并注册所有实现了 <see cref="IGlobalSignalListener"/> 的类型。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">需要扫描的程序集，用于自动发现处理器、管道行为和信号监听器。</param>
    /// <returns>服务集合，支持链式调用。</returns>
    /// <remarks>
    /// 此方法会在 DI 容器中注册所有找到的全局信号监听器，它们会自动应用到所有 NotSignalObject 实例。
    /// 全局信号监听器可用于实现日志记录、审计、监控等横切关注点。
    /// </remarks>
    public static IServiceCollection AddNotMediatorWithGlobalSignalListeners(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddNotMediatorWithPipelineBehaviors(assemblies);
        
        // 自动注册所有实现了 IGlobalSignalListener 的类
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (!type.IsAbstract && !type.IsInterface && 
                type.GetInterfaces().Contains(typeof(IGlobalSignalListener)))
            {
                services.AddSingleton<IGlobalSignalListener>(sp => (IGlobalSignalListener)Activator.CreateInstance(type)!);
            }
        }
        
        return services;
    }

}