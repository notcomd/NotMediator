namespace NotMediator;

public delegate Task RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);