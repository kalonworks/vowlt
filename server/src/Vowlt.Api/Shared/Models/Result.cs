namespace Vowlt.Api.Shared.Models;

public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    // Factory methods for creating results
    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error,
        Errors = [error]
    };

    public static Result<T> Failure(List<string> errors) => new()
    {
        IsSuccess = false,
        Error = errors.FirstOrDefault(),
        Errors = errors
    };
}
public record Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = [];

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error,
        Errors = [error]
    };

    public static Result Failure(List<string> errors) => new()
    {
        IsSuccess = false,
        Error = errors.FirstOrDefault(),
        Errors = errors
    };
}

