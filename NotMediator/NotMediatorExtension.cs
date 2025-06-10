using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

public static class NotMediatorExtension
{
    public static IServiceCollection AddNotMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<INotMediator, NotMediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<INotMediator>();
        mediator.RegisterAllRequestHandler(assemblies);
        mediator.RegisterAllNotificationHandler(assemblies);
        return services;
    }

    private static void RegisterAllRequestHandler(this INotMediator mediator, params Assembly[] assemblies)
    {
        if (mediator == null) throw new ArgumentNullException(nameof(mediator));


        var mediatorType = mediator.GetType();
        var registerMethod = mediatorType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "RegisterRequestHandler" && m.IsGenericMethod && m.GetGenericArguments().Length == 2);
        if (registerMethod == null)
        {
            throw new InvalidOperationException("INotMediator implementation does not have a public RegisterRequestHandler method.");
        }


        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new
                {
                    HandlerType = t,
                    RequestType = i.GetGenericArguments()[0],
                    ResponseType = i.GetGenericArguments()[1],
                    InterfaceType = i
                })).ToList();
        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handlerInstance = Activator.CreateInstance(handlerType.HandlerType);
                var genericRegisterMethod = registerMethod.MakeGenericMethod(handlerType.RequestType, handlerType.ResponseType);
                genericRegisterMethod.Invoke(mediator, new[]
                {
                    handlerInstance
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering handler {handlerType.HandlerType}: {ex.Message}");
            }
        }
    }

    private static void RegisterAllNotificationHandler(this INotMediator mediator, params Assembly[] assemblies)
    {

        if (mediator is null) throw new ArgumentNullException(nameof(mediator));

        var mediatorType = mediator.GetType();
        var registerMethod = mediatorType.GetMethods(
            BindingFlags.Instance | BindingFlags.Public
        ).FirstOrDefault(m => m.Name == "RegisterNotificationHandler" && m.IsGenericMethod && m.GetGenericArguments().Length == 1);

        //var methodInfo = typeof(NotMediator).GetMethod("RegisterNotificationHandler",BindingFlags.Instance | BindingFlags.Public);
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => new
                {
                    HandlerType = t,
                    RequestType = i.GetGenericArguments()[0],
                    InterfaceType = i
                })).ToList();

        if (handlerTypes is null) throw new ArgumentNullException(nameof(handlerTypes));

        foreach (var type in handlerTypes)
        {
            try
            {
                var handlerInstance = Activator.CreateInstance(type.HandlerType);
                var genericMethodInfo = registerMethod!.MakeGenericMethod(type.RequestType);
                genericMethodInfo.Invoke(mediator, [handlerInstance]);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error registering handler {type.HandlerType}: {e.Message}");
                throw;
            }

        }
    }
}