namespace PulseChat.Domain.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    protected Result(bool isSuccess, string? errorCode, string? message)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Message = message;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string errorCode, string message) => new(false, errorCode, message);

    public static Result<T> Success<T>(T data) => Result<T>.Success(data);

    public static Result<T> Failure<T>(string errorCode, string message) => Result<T>.Failure(errorCode, message);
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    protected Result(bool isSuccess, T? data, string? errorCode, string? message)
        : base(isSuccess, errorCode, message)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, null, null);

    public new static Result<T> Failure(string errorCode, string message) => new(false, default, errorCode, message);
}
