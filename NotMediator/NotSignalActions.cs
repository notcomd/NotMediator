namespace NotMediator;

public class NotSignalActions(Action action) : NotSignalAction
{
    private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke() => _action.Invoke();

    public static NotSignalActions Create(Action action)
    {
        return new NotSignalActions(action);
    }
}

public class NotSignalActions<T>(Action<T> action) : NotSignalAction
{
    private readonly Action<T> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T t)
    {
        _action.Invoke(t);
    }

    public static NotSignalActions<T> Create(Action<T> action)
    {
        return new NotSignalActions<T>(action);
    }
}

public class NotSignalActions<T1, T2>(Action<T1, T2> action) : NotSignalAction
{
    private readonly Action<T1, T2> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T1 t1, T2 t2)
    {
        _action.Invoke(t1, t2);
    }

    public static NotSignalActions<T1, T2> Create(Action<T1, T2> action)
    {
        return new NotSignalActions<T1, T2>(action);
    }
}

public class NotSignalActions<T1, T2, T3>(Action<T1, T2, T3> action) : NotSignalAction
{
    private readonly Action<T1, T2, T3> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T1 t1, T2 t2, T3 t3)
    {
        _action.Invoke(t1, t2, t3);
    }

    public static NotSignalActions<T1, T2, T3> Create(Action<T1, T2, T3> action)
    {
        return new NotSignalActions<T1, T2, T3>(action);
    }
}

public class NotSignalActions<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) : NotSignalAction
{
    private readonly Action<T1, T2, T3, T4> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        _action.Invoke(t1, t2, t3, t4);
    }

    public static NotSignalActions<T1, T2, T3, T4> Create(Action<T1, T2, T3, T4> action)
    {
        return new NotSignalActions<T1, T2, T3, T4>(action);
    }
}

public class NotSignalActions<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action) : NotSignalAction
{
    private readonly Action<T1, T2, T3, T4, T5> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
    {
        _action.Invoke(t1, t2, t3, t4, t5);
    }

    public static NotSignalActions<T1, T2, T3, T4, T5> Create(Action<T1, T2, T3, T4, T5> action)
    {
        return new NotSignalActions<T1, T2, T3, T4, T5>(action);
    }
}

public class NotSignalActions<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action) : NotSignalAction
{
    private readonly Action<T1, T2, T3, T4, T5, T6> _action = action ?? throw new ArgumentNullException(nameof(action));

    protected override Delegate Handler => _action;

    public override Type GetHandlerType()
    {
        return HandlerType;
    }

    public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
    {
        _action.Invoke(t1, t2, t3, t4, t5, t6);
    }

    public static NotSignalActions<T1, T2, T3, T4, T5, T6> Create(Action<T1, T2, T3, T4, T5, T6> action)
    {
        return new NotSignalActions<T1, T2, T3, T4, T5, T6>(action);
    }
}