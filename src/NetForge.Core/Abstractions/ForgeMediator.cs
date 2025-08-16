namespace NetForge.Core.Abstractions;

public interface IForgeRequest<TResponse> { }
public interface IForgeRequestHandler<TRequest, TResponse> where TRequest : IForgeRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IForgeMediator
{
    Task<TResponse> Send<TResponse>(IForgeRequest<TResponse> request, CancellationToken ct = default);
}

public delegate Task<TResponse> ForgeRequestHandlerDelegate<TResponse>();

public interface IForgePipelineBehavior<TRequest, TResponse> where TRequest : IForgeRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, ForgeRequestHandlerDelegate<TResponse> next, CancellationToken ct);
}

public sealed class ForgeMediator : IForgeMediator
{
    private readonly IServiceProvider _sp;
    public ForgeMediator(IServiceProvider sp) => _sp = sp;

    public Task<TResponse> Send<TResponse>(IForgeRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var handlerType = typeof(IForgeRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _sp.GetService(handlerType) ?? throw new InvalidOperationException($"Handler not registered for {requestType.Name}");

        var method = handlerType.GetMethod("Handle") ?? throw new InvalidOperationException("Handle method not found");
        var behaviorsServiceType = typeof(IEnumerable<>).MakeGenericType(typeof(IForgePipelineBehavior<,>).MakeGenericType(requestType, responseType));
        var behaviors = ((IEnumerable<object>?)_sp.GetService(behaviorsServiceType)) ?? Array.Empty<object>();
        var ordered = behaviors.Reverse().ToList();

        Task<TResponse> CoreInvoke()
        {
            var result = method.Invoke(handler, new object?[] { request, ct });
            return result is Task<TResponse> typed
                ? typed
                : throw new InvalidOperationException("Handler returned unexpected result type");
        }

        ForgeRequestHandlerDelegate<TResponse> next = () => CoreInvoke();
        foreach (var behaviorObj in ordered)
        {
            var handleMethod = behaviorObj.GetType().GetMethod("Handle") ?? throw new InvalidOperationException("Behavior Handle missing");
            var currentNext = next;
            next = () => (Task<TResponse>)handleMethod.Invoke(behaviorObj, new object?[] { request, currentNext, ct })!;
        }
        return next();
    }
}
