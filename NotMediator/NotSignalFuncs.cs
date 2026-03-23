namespace NotMediator;

public class NotSignalFuncs<TResult>(Func<Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync()
    {
        return func.Invoke();
    }

    public static NotSignalFuncs<TResult> Create(Func<Task<TResult>> func)
    {
        return new(func);
    }
}

public class NotSignalFuncs<T, TResult>(Func<T, Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T param)
    {
        return func.Invoke(param);
    }

    public static NotSignalFuncs<T, TResult> Create(Func<T, Task<TResult>> func)
    {
        return new(func);
    }
}

public class NotSignalFuncs<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T1 param1, T2 param2)
    {
        return func.Invoke(param1, param2);
    }

    public static NotSignalFuncs<T1, T2, TResult> Create(Func<T1, T2, Task<TResult>> func)
    {
        return new(func);
    }
}

public class NotSignalFuncs<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T1 param1, T2 param2, T3 param3)
    {
        return func.Invoke(param1, param2, param3);
    }

    public static NotSignalFuncs<T1, T2, T3, TResult> Create(Func<T1, T2, T3, Task<TResult>> func) => new(func);
}

public class NotSignalFuncs<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T1 param1, T2 param2, T3 param3, T4 param4)
    {
        return func.Invoke(param1, param2, param3, param4);
    }

    public static NotSignalFuncs<T1, T2, T3, T4, TResult> Create(Func<T1, T2, T3, T4, Task<TResult>> func) => new(func);
}

public class NotSignalFuncs<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> func) : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
    {
        return func.Invoke(param1, param2, param3, param4, param5);
    }

    public static NotSignalFuncs<T1, T2, T3, T4, T5, TResult> Create(Func<T1, T2, T3, T4, T5, Task<TResult>> func) =>
        new(func);
}

public class NotSignalFuncs<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> func)
    : NotSignalAction
{
    protected override Delegate Handler => func;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public Task<TResult> InvokeAsync(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
    {
        return func.Invoke(param1, param2, param3, param4, param5, param6);
    }

    public static NotSignalFuncs<T1, T2, T3, T4, T5, T6, TResult> Create(
        Func<T1, T2, T3, T4, T5, T6, Task<TResult>> func) =>
        new(func);
}