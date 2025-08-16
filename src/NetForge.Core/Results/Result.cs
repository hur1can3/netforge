namespace NetForge.Core.Results;

public readonly struct ForgeResult
{
    private ForgeResult(bool success, IReadOnlyList<ForgeError>? errors)
    {
        IsSuccess = success;
        Errors = errors ?? Array.Empty<ForgeError>();
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<ForgeError> Errors { get; }

    public static ForgeResult Success() => new(true, null);
    public static ForgeResult Failure(params ForgeError[] errors) => new(false, errors);
    public static ForgeResult Failure(IEnumerable<ForgeError> errors) => new(false, errors.ToList());
}

public readonly struct ForgeResult<T>
{
    private ForgeResult(bool success, T? value, IReadOnlyList<ForgeError>? errors)
    {
        IsSuccess = success;
        Value = value!;
        Errors = errors ?? Array.Empty<ForgeError>();
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public IReadOnlyList<ForgeError> Errors { get; }

    public static ForgeResult<T> Success(T value) => new(true, value, null);
    public static ForgeResult<T> Failure(params ForgeError[] errors) => new(false, default, errors);
    public static ForgeResult<T> Failure(IEnumerable<ForgeError> errors) => new(false, default, errors.ToList());
}
