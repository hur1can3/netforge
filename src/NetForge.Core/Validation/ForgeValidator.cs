using NetForge.Core.Results;
using NetForge.Core.Abstractions;

namespace NetForge.Core.Validation;

public abstract class ForgeValidator<T>
{
    private readonly List<ForgeError> _errors = new();
    protected void RuleFor(bool condition, ForgeError error)
    {
        if (condition) _errors.Add(error);
    }
    protected abstract void OnValidate(T instance);

    public ForgeResult Validate(T instance)
    {
        _errors.Clear();
        OnValidate(instance);
        return _errors.Count == 0 ? ForgeResults.Success() : ForgeResults.Failure(_errors);
    }
}

// Pipeline behavior that performs validation and short-circuits on failure for ForgeResult / ForgeResult<T> responses.
public sealed class ForgeValidationBehavior<TRequest, TResponse> : ForgePipelineBehavior<TRequest, TResponse>
    where TRequest : ForgeRequest<TResponse>
{
    private readonly IServiceProvider _sp;
    public ForgeValidationBehavior(IServiceProvider sp) => _sp = sp;

    public override Task<TResponse> Handle(TRequest request, ForgeRequestHandlerExecution<TResponse> nextHandler, CancellationToken ct)
    {
        // Try resolve validator; if none, continue
        var validatorType = typeof(ForgeValidator<>).MakeGenericType(typeof(TRequest));
        var validator = _sp.GetService(validatorType);
        if (validator is null)
        {
            return nextHandler();
        }

        // Invoke Validate via reflection
        var validateMethod = validatorType.GetMethod("Validate")!;
        var validationResultObj = validateMethod.Invoke(validator, new object?[] { request })!;
        var isSuccessProp = validationResultObj.GetType().GetProperty("IsSuccess")!;
        bool isSuccess = (bool)isSuccessProp.GetValue(validationResultObj)!;
        if (isSuccess)
        {
            return nextHandler();
        }

        // Short-circuit: map validation result to TResponse
        var errorsProp = validationResultObj.GetType().GetProperty("Errors")!;
        var errors = (IEnumerable<ForgeError>)errorsProp.GetValue(validationResultObj)!;

        var responseType = typeof(TResponse);
        object failureResponse;
        if (responseType == typeof(ForgeResult))
        {
            // Already a ForgeResult
            failureResponse = validationResultObj;
        }
        else if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ForgeResult<>))
        {
            var genericArg = responseType.GetGenericArguments()[0];
            // ForgeResults.Failure<T>(IEnumerable<ForgeError>)
            var failureGenericMethod = typeof(ForgeResults).GetMethods()
                .Where(m => m.Name == nameof(ForgeResults.Failure) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
                .First(m => m.GetParameters()[0].ParameterType == typeof(IEnumerable<ForgeError>))
                .MakeGenericMethod(genericArg);
            failureResponse = failureGenericMethod.Invoke(null, new object?[] { errors })!;
        }
        else
        {
            throw new InvalidOperationException($"Validation behavior can only be used when response type is ForgeResult or ForgeResult<T>. Actual: {responseType.Name}");
        }

        return Task.FromResult((TResponse)failureResponse);
    }
}
