namespace NetForge.Core.Abstractions;

public interface IRequest<TResponse> { }
public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public interface IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    public Mediator(IServiceProvider sp) => _sp = sp;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = _sp.GetService(handlerType) ?? throw new InvalidOperationException($"Handler not registered for {requestType.Name}");

        var behaviorsServiceType = typeof(IEnumerable<>).MakeGenericType(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse)));
        var behaviors = ((IEnumerable<object>?)_sp.GetService(behaviorsServiceType)) ?? Array.Empty<object>();
        var ordered = behaviors.Cast<dynamic>().Reverse().ToList();
        RequestHandlerDelegate<TResponse> terminal = () => ((dynamic)handler).Handle((dynamic)request, ct);
        var chain = ordered.Aggregate(terminal, (next, behavior) => () => behavior.Handle((dynamic)request, next, ct));
        return chain();
    }
}
