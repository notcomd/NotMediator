namespace NotMediator;

public abstract class NotSignalObject
{
    
    protected readonly NotSignalManager NotSignalManager= new NotSignalManager();
    
    
    public void Connect(string signalName, Delegate handler)
    {
        NotSignalManager.Connect(signalName, handler);
    }
    
    
    public void Disconnect(string signalName, Delegate handler)
    {
        NotSignalManager.Disconnect(signalName, handler);
    }
    
    public void EmitSignal(string signalName, params object[] signalData)
    {
        NotSignalManager.Emit(signalName, signalData);
    }
    
}