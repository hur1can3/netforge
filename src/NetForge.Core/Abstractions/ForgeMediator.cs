using System.Reflection;
using System.Linq;

namespace NetForge.Core.Abstractions;

// Requests
public abstract record ForgeRequest<TResponse>;

// Core handler
public abstract class ForgeRequestHandler<TRequest, TResponse> where TRequest : ForgeRequest<TResponse>
{
    public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

// Existing behavior (around handler) – remains for cross-cutting concerns requiring wrapping semantics
public delegate Task<TResponse> ForgeRequestHandlerExecution<TResponse>(); // Intentionally ends with Execution to satisfy CA1711

public abstract class ForgePipelineBehavior<TRequest, TResponse> where TRequest : ForgeRequest<TResponse>
{
    public abstract Task<TResponse> Handle(TRequest request, ForgeRequestHandlerExecution<TResponse> nextHandler, CancellationToken ct);
}

// New: Pre-processors (execute before any behaviors/handler)
public interface IForgePreProcessor<TRequest, TResponse> where TRequest : ForgeRequest<TResponse>
{
    Task PreProcess(TRequest request, CancellationToken ct);
}

// New: Post-processors (execute after successful handler + behaviors)
public interface IForgePostProcessor<TRequest, TResponse> where TRequest : ForgeRequest<TResponse>
{
    Task PostProcess(TRequest request, TResponse response, CancellationToken ct);
}

// New: Exception handlers (attempt to translate exceptions into a response)
public interface IForgeExceptionHandler<TRequest, TResponse> where TRequest : ForgeRequest<TResponse>
{
    Task<ForgeExceptionHandlingResult<TResponse>> HandleException(TRequest request, Exception exception, CancellationToken ct);
}

public readonly struct ForgeExceptionHandlingResult<TResponse>
{
    public ForgeExceptionHandlingResult(bool handled, TResponse? response)
    {
        Handled = handled;
        Response = response!; // null only if not handled
    }
    public bool Handled { get; }
    public TResponse Response { get; }
    public static ForgeExceptionHandlingResult<TResponse> NotHandled() => new(false, default);
    public static ForgeExceptionHandlingResult<TResponse> HandledResult(TResponse response) => new(true, response);
}

// Mediator abstraction
public interface IForgeMediator
{
    Task<TResponse> Send<TResponse>(ForgeRequest<TResponse> request, CancellationToken ct = default);
}

public sealed class ForgeMediator : IForgeMediator
{
    private readonly IServiceProvider _sp;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type Request, Type Response), HandlerPlan> _planCache = new();

    private sealed record HandlerPlan(
        Type RequestType,
        Type ResponseType,
        object HandlerInstance,
        MethodInfo HandleMethod,
        Type PipelineBehaviorClosedGeneric,
        Type PreProcessorClosedGeneric,
        Type PostProcessorClosedGeneric,
        Type ExceptionHandlerClosedGeneric
    );

    public ForgeMediator(IServiceProvider sp) => _sp = sp;

    public Task<TResponse> Send<TResponse>(ForgeRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var plan = _planCache.GetOrAdd((requestType, responseType), key => BuildPlan(key.Request, key.Response));

        var handlerType = typeof(ForgeRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _sp.GetService(handlerType) ?? throw new InvalidOperationException($"Handler not registered for {requestType.Name}");

        var method = plan.HandleMethod;

        var behaviorsServiceType = typeof(IEnumerable<>).MakeGenericType(plan.PipelineBehaviorClosedGeneric);
        var behaviors = ((IEnumerable<object>?)_sp.GetService(behaviorsServiceType)) ?? Array.Empty<object>();
        var orderedBehaviors = behaviors.Reverse().ToList();

        var preType = typeof(IEnumerable<>).MakeGenericType(plan.PreProcessorClosedGeneric);
        var preProcessors = ((IEnumerable<object>?)_sp.GetService(preType)) ?? Array.Empty<object>();

        var postType = typeof(IEnumerable<>).MakeGenericType(plan.PostProcessorClosedGeneric);
        var postProcessors = ((IEnumerable<object>?)_sp.GetService(postType)) ?? Array.Empty<object>();

        var exType = typeof(IEnumerable<>).MakeGenericType(plan.ExceptionHandlerClosedGeneric);
        var exceptionHandlers = ((IEnumerable<object>?)_sp.GetService(exType)) ?? Array.Empty<object>();

        async Task<TResponse> CoreInvoke()
        {
            foreach (var pre in preProcessors)
            {
                var preMethod = pre.GetType().GetMethod("PreProcess") ?? throw new InvalidOperationException("PreProcess method missing");
                var preTask = (Task?)preMethod.Invoke(pre, new object?[] { request, ct });
                if (preTask is not null) await preTask.ConfigureAwait(false);
            }

            Task<TResponse> InvokeHandler()
            {
                var result = method.Invoke(handler, new object?[] { request, ct });
                return result is Task<TResponse> typed
                    ? typed
                    : throw new InvalidOperationException("Handler returned unexpected result type");
            }

            ForgeRequestHandlerExecution<TResponse> next = () => InvokeHandler();
            foreach (var behaviorObj in orderedBehaviors)
            {
                var handleMethod = behaviorObj.GetType().GetMethod("Handle") ?? throw new InvalidOperationException("Behavior Handle missing");
                var currentNext = next;
                next = () => (Task<TResponse>)handleMethod.Invoke(behaviorObj, new object?[] { request, currentNext, ct })!;
            }

            var response = await next().ConfigureAwait(false);

            foreach (var post in postProcessors)
            {
                var postMethod = post.GetType().GetMethod("PostProcess") ?? throw new InvalidOperationException("PostProcess method missing");
                var postTask = (Task?)postMethod.Invoke(post, new object?[] { request, response, ct });
                if (postTask is not null) await postTask.ConfigureAwait(false);
            }
            return response;
        }

        return ExecuteWithExceptionHandling();

        async Task<TResponse> ExecuteWithExceptionHandling()
        {
            try
            {
                return await CoreInvoke().ConfigureAwait(false);
            }
            catch (Exception ex) when (exceptionHandlers.Any())
            {
                foreach (var handlerObj in exceptionHandlers)
                {
                    var exHandleMethod = handlerObj.GetType().GetMethod("HandleException") ?? throw new InvalidOperationException("HandleException method missing");
                    var taskObj = exHandleMethod.Invoke(handlerObj, new object?[] { request, ex, ct });
                    if (taskObj is Task task)
                    {
                        await task.ConfigureAwait(false);
                        var resultProp = task.GetType().GetProperty("Result");
                        var handlingResult = resultProp?.GetValue(task);
                        if (handlingResult is not null)
                        {
                            var handledProp = handlingResult.GetType().GetProperty("Handled")!;
                            var handled = (bool)handledProp.GetValue(handlingResult)!;
                            if (handled)
                            {
                                var responseProp = handlingResult.GetType().GetProperty("Response")!;
                                var resp = responseProp.GetValue(handlingResult);
                                if (resp is TResponse typedResponse)
                                {
                                    return typedResponse;
                                }
                                throw new InvalidOperationException("Exception handler returned unexpected response type");
                            }
                        }
                    }
                }
                throw;
            }
        }
    }

    private static HandlerPlan BuildPlan(Type requestType, Type responseType)
    {
        var handlerType = typeof(ForgeRequestHandler<,>).MakeGenericType(requestType, responseType);
        var method = handlerType.GetMethod("Handle") ?? throw new InvalidOperationException("Handle method not found");
        var pipelineBehaviorClosed = typeof(ForgePipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var preClosed = typeof(IForgePreProcessor<,>).MakeGenericType(requestType, responseType);
        var postClosed = typeof(IForgePostProcessor<,>).MakeGenericType(requestType, responseType);
        var exClosed = typeof(IForgeExceptionHandler<,>).MakeGenericType(requestType, responseType);
        return new HandlerPlan(requestType, responseType, HandlerInstance: null!, method, pipelineBehaviorClosed, preClosed, postClosed, exClosed);
    }
}
