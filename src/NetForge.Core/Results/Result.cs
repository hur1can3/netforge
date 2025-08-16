namespace NetForge.Core.Results;

public readonly struct Result
{
    private Result(bool success, IReadOnlyList<Error>? errors)
    {
        IsSuccess = success;
        Errors = errors ?? Array.Empty<Error>();
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(params Error[] errors) => new(false, errors);
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToList());
}

public readonly struct Result<T>
{
    private Result(bool success, T? value, IReadOnlyList<Error>? errors)
    {
        IsSuccess = success;
        Value = value!;
        Errors = errors ?? Array.Empty<Error>();
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(params Error[] errors) => new(false, default, errors);
    public static Result<T> Failure(IEnumerable<Error> errors) => new(false, default, errors.ToList());
}
