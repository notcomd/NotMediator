using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

public static class NotMediatorExtension
{
    public static IServiceCollection AddNotMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        // 注册 NotMediator 并注入 IServiceProvider
        services.AddSingleton<INotMediator, NotMediator>(sp => new NotMediator(sp));

        // 自动注册所有处理器
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
