namespace NotMediator;

public interface IPipelineBehavior<in TRequest, TResponse>
{
    
    Task<TResponse> Handler(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
    
}