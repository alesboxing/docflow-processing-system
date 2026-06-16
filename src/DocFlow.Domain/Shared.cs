namespace DocFlow.Domain.Shared;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

public sealed record AppError(string Code, string Message, ErrorType Type);

public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public AppError? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(AppError error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, null) => _value = value;
    private Result(AppError error) : base(false, error) { }

    public T Value => IsSuccess && _value is not null
        ? _value
        : throw new InvalidOperationException("Result has no value.");

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(AppError error) => new(error);
}
