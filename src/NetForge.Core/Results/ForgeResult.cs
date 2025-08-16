namespace NetForge.Core.Results;

public readonly struct ForgeResult : IEquatable<ForgeResult>
{
    private ForgeResult(bool success, IReadOnlyList<ForgeError>? errors)
    {
        IsSuccess = success;
        Errors = errors ?? Array.Empty<ForgeError>();
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<ForgeError> Errors { get; }

    public bool Equals(ForgeResult other) => IsSuccess == other.IsSuccess && Errors.SequenceEqual(other.Errors);
    public override bool Equals(object? obj) => obj is ForgeResult fr && Equals(fr);
    public override int GetHashCode() => HashCode.Combine(IsSuccess, Errors.Count);
    public static bool operator ==(ForgeResult left, ForgeResult right) => left.Equals(right);
    public static bool operator !=(ForgeResult left, ForgeResult right) => !left.Equals(right);

    internal static ForgeResult CreateSuccess() => new(true, null);
    internal static ForgeResult CreateFailure(IEnumerable<ForgeError> errors) => new(false, errors.ToList());
}

public readonly struct ForgeResult<T> : IEquatable<ForgeResult<T>>
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

    public bool Equals(ForgeResult<T> other)
        => IsSuccess == other.IsSuccess && EqualityComparer<T>.Default.Equals(Value, other.Value) && Errors.SequenceEqual(other.Errors);
    public override bool Equals(object? obj) => obj is ForgeResult<T> fr && Equals(fr);
    public override int GetHashCode() => HashCode.Combine(IsSuccess, Value, Errors.Count);
    public static bool operator ==(ForgeResult<T> left, ForgeResult<T> right) => left.Equals(right);
    public static bool operator !=(ForgeResult<T> left, ForgeResult<T> right) => !left.Equals(right);

    internal static ForgeResult<T> CreateSuccess(T value) => new(true, value, null);
    internal static ForgeResult<T> CreateFailure(IEnumerable<ForgeError> errors) => new(false, default, errors.ToList());
}

public static class ForgeResults
{
    public static ForgeResult Success() => ForgeResult.CreateSuccess();
    public static ForgeResult Failure(params ForgeError[] errors) => ForgeResult.CreateFailure(errors);
    public static ForgeResult Failure(IEnumerable<ForgeError> errors) => ForgeResult.CreateFailure(errors);

    public static ForgeResult<T> Success<T>(T value) => ForgeResult<T>.CreateSuccess(value);
    public static ForgeResult<T> Failure<T>(params ForgeError[] errors) => ForgeResult<T>.CreateFailure(errors);
    public static ForgeResult<T> Failure<T>(IEnumerable<ForgeError> errors) => ForgeResult<T>.CreateFailure(errors);
}
