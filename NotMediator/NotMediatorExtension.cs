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
}
