namespace NotMediator;

public abstract class NotSignalAction

{
    protected abstract Delegate Handler { get; }
    
    public Type HandlerType => Handler.GetType();
    
    public abstract Type GetHandlerType();
    
}