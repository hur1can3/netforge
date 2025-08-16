using NetForge.Core.Results;

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
    return _errors.Count == 0 ? ForgeResult.Success() : ForgeResult.Failure(_errors);
    }
}
