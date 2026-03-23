using Microsoft.Extensions.DependencyInjection;

namespace NotMediator;

/// <summary>
/// 信号对象工厂 - 用于创建已初始化全局监听器的 NotSignalObject 实例
/// </summary>
public static class SignalObjectFactory
{
    private static IServiceProvider? _serviceProvider;
    
    /// <summary>
    /// 初始化服务提供者（在应用启动时调用）
    /// </summary>
    /// <param name="provider">服务提供者</param>
    public static void Initialize(IServiceProvider provider)
    {
        _serviceProvider = provider;
    }
    
    /// <summary>
    /// 创建并初始化 NotSignalObject 实例
    /// </summary>
    /// <typeparam name="T">NotSignalObject 的子类类型</typeparam>
    /// <returns>初始化后的实例</returns>
    public static T Create<T>() where T : NotSignalObject, new()
    {
        if (_serviceProvider is null)
            throw new InvalidOperationException("请先调用 Initialize 方法初始化服务提供者");
            
        var instance = new T();
        instance.InitializeGlobalListeners(_serviceProvider);
        return instance;
    }
    
    /// <summary>
    /// 创建并初始化 NotSignalObject 实例（从服务解析）
    /// </summary>
    /// <typeparam name="T">NotSignalObject 的子类类型</typeparam>
    /// <returns>初始化后的实例</returns>
    public static T Resolve<T>() where T : NotSignalObject
    {
        if (_serviceProvider is null)
            throw new InvalidOperationException("请先调用 Initialize 方法初始化服务提供者");
            
        var instance = _serviceProvider.GetRequiredService<T>();
        instance.InitializeGlobalListeners(_serviceProvider);
        return instance;
    }
}
