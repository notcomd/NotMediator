namespace NotMediator;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handler(TRequest request, CancellationToken cancellationToken);
}